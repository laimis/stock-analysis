using System;
using System.Linq;
using core.fs.Shared.Domain;
using core.Shared;
using core.Stocks;
using coretests.testdata;
using Microsoft.FSharp.Core;
using Xunit;

namespace coretests.Stocks
{
    public class StockPositionTests
    {
        private static readonly Guid _userId = TestDataGenerator.RandomUserId().Item;
        private static readonly Ticker _ticker = TestDataGenerator.TEUM;
        
        private static (StockPositionState, StockPositionWithCalculations) WithCalculations(
            Func<StockPositionState> func)
        {
            var result = func();
            return (result, new StockPositionWithCalculations(result));
        }
            
        [Fact]
        public void PurchaseWorks()
        {
            var (initialStock, initialCalculations) = WithCalculations(
                () => StockPosition.openLong(_ticker, 10, 2.1m, DateTime.UtcNow, null, null)
            );
            
            Assert.Equal(_ticker, initialCalculations.Ticker);
            Assert.Equal(10, initialStock.NumberOfShares);
            Assert.Equal(10, initialCalculations.NumberOfShares);
            Assert.Equal(2.1m, initialCalculations.AverageCostPerShare);
            Assert.Equal(21, initialCalculations.Cost);

            var (afterBuy, afterBuyCalculations) =
                WithCalculations(
                    () => StockPosition.buy(5, 2m, DateTimeOffset.Now, null, initialStock)
                );

            Assert.Equal(15, afterBuy.NumberOfShares);
            Assert.Equal(2.07m, afterBuyCalculations.AverageCostPerShare, 2);
            Assert.Equal(31, afterBuyCalculations.Cost);

            var (afterSell, _) =
                WithCalculations(
                    () => StockPosition.sell(5, 20, DateTimeOffset.Now, "sample note", afterBuy)
                );

            Assert.Equal(10, afterSell.NumberOfShares);
        }

        [Fact]
        public void SellingNotOwnedFails()
        {
            var stock = StockPosition.openLong(_ticker, 10, 2.1m, DateTime.UtcNow, null, null);

            Assert.ThrowsAny<Exception>(() => StockPosition.sell(20, 100, DateTime.UtcNow, "sample note", stock));
        }

        [Fact]
        public void BuyingForZeroThrows()
        {
            Assert.Throws<Exception>(() =>
                StockPosition.openLong(TestDataGenerator.TSLA, 10, 0m, DateTime.UtcNow, null, null)
            );
        }

        [Fact]
        public void BuyingWithBadDateThrows()
        {
            Assert.Throws<Exception>(() => 
                StockPosition.openLong(TestDataGenerator.TSLA, 10, 10m, default, null, null)
            );
        }

        [Fact]
        public void PurchaseWithDateNotProvidedThrows()
        {
            var stock = StockPosition.openLong(TestDataGenerator.TSLA, 10, 10m, DateTime.UtcNow, null, null);

            Assert.Throws<Exception>(() => StockPosition.buy(1, 20, DateTimeOffset.MinValue, null, stock));
        }

        [Fact]
        public void Purchase_WithDateInTheFuture_Throws()
        {
            var stock = StockPosition.openLong(TestDataGenerator.TSLA, 10, 10m, DateTime.UtcNow, null, null);
            
            Assert.Throws<Exception>(() => StockPosition.buy(1, 20, DateTimeOffset.UtcNow.AddDays(1), null, stock));
        }

        [Fact]
        public void EventCstrReplaysEvents()
        {
            var stock = StockPosition.openLong(_ticker, 10, 2.1m, DateTime.UtcNow, null, null);

            var events = stock.Events;

            var stock2 = StockPosition.createFromEvents(events);

            Assert.Equal(stock.NumberOfShares, stock2.NumberOfShares);
        }

        [Fact]
        public void MultipleBuysAverageCostCorrect()
        {
            var stock = StockPosition.openLong(_ticker, 1, 5m, DateTime.UtcNow, null, null);

            var purchase1 = StockPosition.buy(1, 10, DateTimeOffset.UtcNow, null, stock);

            Assert.Equal(7.5m, purchase1.AverageCostPerShare);
            Assert.Equal(15, purchase1.Cost);

            var sell1 = StockPosition.sell(1, 6, DateTimeOffset.UtcNow, null, purchase1);

            Assert.Equal(10m, sell1.AverageCostPerShare);
            Assert.Equal(10m, sell1.Cost);

            var purchase2 = StockPosition.buy(1, 10, DateTimeOffset.UtcNow, null, sell1);

            Assert.Equal(10m, purchase2.AverageCostPerShare);
            Assert.Equal(20m, purchase2.Cost);

            var sell2 = StockPosition.sell(2, 10, DateTimeOffset.UtcNow, null, purchase2);

            Assert.True(FSharpOption<DateTimeOffset>.get_IsSome(sell2.Closed));

            Assert.Equal(0, sell2.DaysHeld);
            Assert.Equal(1, sell2.Profit);
            // Assert.Equal(0.04m, sell2.GainPct, 2);
        }

        [Fact]
        public void SellCreatesPLTransaction()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            // buying two shares one at a time, average cost should be 7.5
            stock.Purchase(1, 5, DateTimeOffset.UtcNow);
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(7.5m, stock.State.OpenPosition.AverageCostPerShare);
            Assert.Equal(15, stock.State.OpenPosition.Cost);

            // sold one for 6, so I should have a profit of 1
            // and then average cost would go up to 10
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var tx = stock.State.Transactions.Last();

            Assert.True(tx.IsPL);
            Assert.Equal(1m, tx.Amount);

            // buy another share for 10, keeps my average cost at 10
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(10m, stock.State.OpenPosition.AverageCostPerShare);
            
            // sell those two for 10. since average cost is also 10, profit transaction is there, but its amount is zero
            stock.Sell(2, 10, DateTimeOffset.UtcNow, null);

            tx = stock.State.Transactions.Last();

            Assert.True(tx.IsPL);
            Assert.Equal(0, tx.Amount);
        }

        [Fact]
        public void MultipleBuysDeletingTransactions()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow);
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(7.5m, stock.State.OpenPosition.AverageCostPerShare);
            Assert.Equal(15, stock.State.OpenPosition.Cost);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Equal(10m, stock.State.OpenPosition.AverageCostPerShare);
            Assert.Equal(10m, stock.State.OpenPosition.Cost);

            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(10m, stock.State.OpenPosition.AverageCostPerShare);
            Assert.Equal(20m, stock.State.OpenPosition.Cost);

            stock.Sell(2, 10, DateTimeOffset.UtcNow, null);

            Assert.Null(stock.State.OpenPosition);

            var last = stock.State.Transactions.Where(t => !t.IsPL).Last();

            stock.DeleteTransaction(last.EventId);

            Assert.NotNull(stock.State.OpenPosition);
        }

        [Fact]
        public void DaysHeldCorrect()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Purchase(1, 10, DateTimeOffset.UtcNow.AddDays(-2));

            Assert.Equal(5, stock.State.OpenPosition.DaysHeld);
            Assert.Equal(2, stock.State.OpenPosition.DaysSinceLastTransaction);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Equal(5, stock.State.OpenPosition.DaysHeld);
            Assert.Equal(0, stock.State.OpenPosition.DaysSinceLastTransaction);

            stock.Sell(1, 10, DateTimeOffset.UtcNow, null);

            Assert.Null(stock.State.OpenPosition);
        }

        [Fact]
        public void PositionId_LogicIsCorrect()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            // first time buying, position should be 0
            // then, once closed out, open position is null
            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            Assert.Equal(0, stock.State.OpenPosition.PositionId);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Null(stock.State.OpenPosition);

            // second buy, the position should be 1
            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            Assert.Equal(1, stock.State.OpenPosition.PositionId);

            // now, delete the buy, which will kill position
            var last = stock.State.Transactions.Where(t => !t.IsPL).Last();

            stock.DeleteTransaction(last.EventId);

            Assert.Null(stock.State.OpenPosition);

            // open a new one, should be fresh new position id
            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            Assert.Equal(2, stock.State.OpenPosition.PositionId);
        }

        [Fact]
        public void AssignGrade_To_OpenPosition_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            Assert.Throws<InvalidOperationException>(() => 
                stock.AssignGrade(0, TestDataGenerator.A, "this trade went perfectly!")
            );
        }

        [Fact]
        public void AssignGrade_To_ClosedPosition_Succeeds()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var positionId = 0;
            stock.AssignGrade(positionId, TestDataGenerator.A, "this trade went perfectly!");

            var position = stock.State.GetPosition(0);

            Assert.Equal(TestDataGenerator.A, position.Grade);
            Assert.Equal("this trade went perfectly!", position.GradeNote);
            Assert.Contains(position.GradeNote, position.Notes);
        }

        [Fact]
        public void AssignGrade_To_ClosedPosition_UpdatesExistingGrade()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var positionId = 0;
            stock.AssignGrade(positionId, TestDataGenerator.A, "this trade went perfectly!");
            stock.AssignGrade(positionId, TestDataGenerator.B, "this trade went perfectly!");

            var position = stock.State.GetPosition(0);
            Assert.Equal(TestDataGenerator.B, position.Grade);
            Assert.Equal("this trade went perfectly!", position.GradeNote);
        }

        [Fact]
        public void AssignGrade_WithUpdatedNote_RemovesPreviousNoteAndAddsNewOne()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var positionId = 0;
            stock.AssignGrade(positionId, TestDataGenerator.A, "this trade went perfectly!");
            stock.AssignGrade(positionId, TestDataGenerator.A, "this trade went perfectly! (updated)");

            var position = stock.State.GetPosition(0);
            Assert.Equal(TestDataGenerator.A, position.Grade);
            Assert.Equal("this trade went perfectly! (updated)", position.GradeNote);
            Assert.DoesNotContain("this trade went perfectly!", position.Notes);
            Assert.Contains("this trade went perfectly! (updated)", position.Notes);
        }

        [Fact]
        public void AssignInvalidGrade_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Throws<ArgumentException>(() => 
                stock.AssignGrade(0, new TradeGrade("L"), "this trade went perfectly!")
            );
        }

        [Fact]
        public void AssigningStop_Works()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            var assigned = stock.SetStop(4);

            Assert.True(assigned);
            Assert.Equal(4, stock.State.OpenPosition.StopPrice);
            
            var secondAssignmentToSameValue = stock.SetStop(4);
            Assert.False(secondAssignmentToSameValue);

            var thirdAssignmentToDifferentValue = stock.SetStop(3);
            Assert.True(thirdAssignmentToDifferentValue);
        }

        [Fact]
        public void AssigningStop_ToClosedPosition_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Throws<InvalidOperationException>(() => 
                stock.SetStop(4)
            );
        }

        [Fact]
        public void AddingNote_Works()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));

            var assigned = stock.AddNotes("this is a note");

            Assert.True(assigned);
            Assert.Contains("this is a note", stock.State.OpenPosition.Notes);

            var secondAssignmentToSameValue = stock.AddNotes("this is a note");
            Assert.False(secondAssignmentToSameValue);

            var thirdAssignmentToDifferentValue = stock.AddNotes("this is a different note");
            Assert.True(thirdAssignmentToDifferentValue);
        }

        [Fact]
        public void AddingNote_ToClosedPosition_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Throws<InvalidOperationException>(() => 
                stock.AddNotes("this is a note")
            );
        }

        [Fact]
        public void DeletePosition_OnClosedPosition_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Throws<InvalidOperationException>(() => 
                stock.DeletePosition(positionId)
            );
        }

        [Fact]
        public void DeletePosition_Works()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId2 = stock.State.OpenPosition.PositionId;
            stock.DeletePosition(positionId2);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId3 = stock.State.OpenPosition.PositionId;
            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var positions = stock.State.GetAllPositions();
            Assert.Equal(2, positions.Count);
            Assert.Equal(positionId, stock.State.GetAllPositions()[0].PositionId);
            Assert.Equal(positionId3, stock.State.GetAllPositions()[1].PositionId);
            
            // make sure transactions don't include deleted position
            Assert.Equal(6, stock.State.Transactions.Count);
            Assert.Equal(2, stock.State.Transactions.Count(t => t.IsPL));
            Assert.Equal(4, stock.State.Transactions.Count(t => !t.IsPL));
        }

        [Fact]
        public void LabelsWork()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            
            var set = stock.SetPositionLabel(positionId, "strategy", "newhigh");
            Assert.True(set);

            var setAgain = stock.SetPositionLabel(positionId, "strategy", "newhigh");
            Assert.False(setAgain);

            var setDifferent = stock.SetPositionLabel(positionId, "strategy", "newlow");
            Assert.True(setDifferent);
        }

        [Fact]
        public void SetLabel_WithNullValue_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            
            Assert.Throws<InvalidOperationException>(() => 
                stock.SetPositionLabel(positionId, "strategy", null)
            );
        }

        [Fact]
        public void SetLabel_WithNullKey_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            
            Assert.Throws<InvalidOperationException>(() => 
                stock.SetPositionLabel(positionId, null, "newhigh")
            );
        }

        [Fact]
        public void DeleteLabel_Works()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            
            var set = stock.SetPositionLabel(positionId, "strategy", "newhigh");
            Assert.True(set);

            var deleted = stock.DeletePositionLabel(positionId, "strategy");
            Assert.True(deleted);

            var deletedAgain = stock.DeletePositionLabel(positionId, "strategy");
            Assert.False(deletedAgain);
        }

        [Fact]
        public void DeleteLabel_WithNullKey_Fails()
        {
            var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            var positionId = stock.State.OpenPosition.PositionId;
            
            Assert.Throws<InvalidOperationException>(() => 
                stock.DeletePositionLabel(positionId, null)
            );
        }

        [Fact]
        public void BuyWithStopAtCost_Works()
        {
            var stock = new OwnedStock(_ticker, _userId);
            stock.Purchase(1, 1, DateTimeOffset.UtcNow, notes:null, stopPrice: 1);
            
            Assert.Null(stock.State.OpenPosition.RiskedAmount);
        }
    }
}