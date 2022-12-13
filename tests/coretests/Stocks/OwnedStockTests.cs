using System;
using System.Linq;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class OwnedStockTests
    {
        private static Guid _userId = Guid.NewGuid();
            
        [Fact]
        public void PurchaseWorks()
        {
            var stock = new OwnedStock("TEUM", _userId);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            Assert.Equal("TEUM", stock.State.Ticker);
            Assert.Equal(_userId, stock.State.UserId);
            Assert.Equal(10, stock.State.OpenPosition.NumberOfShares);
            Assert.Equal(21, stock.State.OpenPosition.Cost);

            stock.Purchase(5, 2, DateTime.UtcNow);

            Assert.Equal(15, stock.State.OpenPosition.NumberOfShares);
            Assert.Equal(31, stock.State.OpenPosition.Cost, 0);

            stock.Sell(5, 20, DateTime.UtcNow, "sample note");

            Assert.Equal(10, stock.State.OpenPosition.NumberOfShares);
        }

        [Fact]
        public void SellingNotOwnedFails()
        {
            var stock = new OwnedStock("TEUM", _userId);

            stock.Purchase(10, 2.1m, DateTime.UtcNow);

            Assert.ThrowsAny<Exception>(() => stock.Sell(20, 100, DateTime.UtcNow, "sample note"));
        }

        [Fact]
        public void BuyingForZeroThrows()
        {
            var stock = new OwnedStock("tlsa", _userId);

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(10, 0, DateTime.UtcNow));
        }

        [Fact]
        public void BuyingWithBadDateThrows()
        {
            var stock = new OwnedStock("tlsa", _userId);

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(10, 0, DateTime.MinValue));
        }

        [Fact]
        public void BuyingWithBadUserThrows()
        {
            Assert.Throws<InvalidOperationException>(() => new OwnedStock("tlsa", Guid.Empty));
        }

        [Fact]
        public void PurchaseWithDateNotProvidedThrows()
        {
            var stock = new OwnedStock("tlsa", _userId);

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(1, 20, DateTime.MinValue));
        }

        [Fact]
        public void EventCstrReplaysEvents()
        {
            var stock = new OwnedStock("tlsa", _userId);

            stock.Purchase(1, 10, DateTime.UtcNow);

            var events = stock.Events;

            var stock2 = new OwnedStock(events);

            Assert.Equal(stock.State.OpenPosition.NumberOfShares, stock2.State.OpenPosition.NumberOfShares);
        }

        [Fact]
        public void MultipleBuysAverageCostCorrect()
        {
            var stock = new OwnedStock("tsla", _userId);

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

            Assert.Single(stock.State.Positions);
            Assert.Equal(0, stock.State.Positions[0].DaysHeld);
            Assert.Equal(1, stock.State.Positions[0].Profit);
            Assert.Equal(0.04m, stock.State.Positions[0].GainPct, 2);
        }

        [Fact]
        public void SellCreatesPLTransaction()
        {
            var stock = new OwnedStock("tsla", _userId);

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
            var stock = new OwnedStock("tsla", _userId);

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
            var stock = new OwnedStock("tsla", _userId);

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
    }
}
