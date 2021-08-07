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

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            Assert.Equal("TEUM", stock.State.Ticker);
            Assert.Equal(_userId, stock.State.UserId);
            Assert.Equal(10, stock.State.Owned);
            Assert.Equal(21, stock.State.Cost);

            stock.Purchase(5, 2, DateTime.UtcNow);

            Assert.Equal(15, stock.State.Owned);
            Assert.Equal(31, stock.State.Cost, 0);

            stock.Sell(5, 20, DateTime.UtcNow, "sample note");

            Assert.Equal(10, stock.State.Owned);
        }

        [Fact]
        public void SellingNotOwnedFails()
        {
            var stock = new OwnedStock("TEUM", _userId);

            stock.Purchase(10, 2.1, DateTime.UtcNow);

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

            Assert.Equal(stock.State.Owned, stock2.State.Owned);
        }

        [Fact]
        public void MultipleBuysAverageCostCorrect()
        {
            var stock = new OwnedStock("tsla", _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow);
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(7.5, stock.State.AverageCost);
            Assert.Equal(15, stock.State.Cost);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Equal(7.5, stock.State.AverageCost);
            Assert.Equal(7.5, stock.State.Cost);

            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(8.75, stock.State.AverageCost);
            Assert.Equal(17.5, stock.State.Cost);

            stock.Sell(2, 10, DateTimeOffset.UtcNow, null);

            Assert.Equal(0, stock.State.Owned);
            Assert.Equal(0, stock.State.AverageCost);
            Assert.Equal(0, stock.State.Cost);
            Assert.Single(stock.State.PositionInstances);
        }

        [Fact]
        public void SellCreatesPLTransaction()
        {
            var stock = new OwnedStock("tsla", _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow);
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(7.5, stock.State.AverageCost);
            Assert.Equal(15, stock.State.Cost);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            var tx = stock.State.Transactions.Last();

            Assert.True(tx.IsPL);
            Assert.Equal(-1.5, tx.Profit);
            Assert.Equal(7.5, tx.Debit);
            Assert.Equal(6, tx.Credit); // average cost is 7.5, selling for 6 is 1.5 loss

            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(8.75, stock.State.AverageCost);
            
            stock.Sell(2, 10, DateTimeOffset.UtcNow, null);

            tx = stock.State.Transactions.Last();

            Assert.True(tx.IsPL);
            Assert.Equal(2.5, tx.Profit);
            Assert.Equal(20, tx.Credit);
            Assert.Equal(17.5, tx.Debit);
        }

        [Fact]
        public void MultipleBuysDeletingTransactions()
        {
            var stock = new OwnedStock("tsla", _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow);
            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(7.5, stock.State.AverageCost);
            Assert.Equal(15, stock.State.Cost);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Equal(7.5, stock.State.AverageCost);
            Assert.Equal(7.5, stock.State.Cost);

            stock.Purchase(1, 10, DateTimeOffset.UtcNow);

            Assert.Equal(8.75, stock.State.AverageCost);
            Assert.Equal(17.5, stock.State.Cost);

            stock.Sell(2, 10, DateTimeOffset.UtcNow, null);

            Assert.Equal(0, stock.State.Owned);
            Assert.Equal(0, stock.State.AverageCost);
            Assert.Equal(0, stock.State.Cost);

            var last = stock.State.Transactions.Where(t => !t.IsPL).Last();

            stock.DeleteTransaction(last.EventId);

            Assert.Equal(2, stock.State.Owned);
            Assert.Equal(8.75, stock.State.AverageCost);
            Assert.Equal(17.5, stock.State.Cost);

            stock.Delete();

            Assert.Equal(0, stock.State.Owned);
            Assert.Equal(0, stock.State.AverageCost);
            Assert.Equal(0, stock.State.AverageCost);
            Assert.Empty(stock.State.Transactions);
        }

        [Fact]
        public void DaysHeldCorrect()
        {
            var stock = new OwnedStock("tsla", _userId);

            stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
            stock.Purchase(1, 10, DateTimeOffset.UtcNow.AddDays(-2));

            Assert.Equal(5, stock.State.DaysHeld);

            stock.Sell(1, 6, DateTimeOffset.UtcNow, null);

            Assert.Equal(5, stock.State.DaysHeld);

            stock.Sell(1, 10, DateTimeOffset.UtcNow, null);

            Assert.Equal(0, stock.State.DaysHeld);
        }
    }
}
