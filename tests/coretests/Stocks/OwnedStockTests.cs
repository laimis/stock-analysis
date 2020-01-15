using System;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class OwnedStockTests
    {
        [Fact]
        public void PurchaseWorks()
        {
            var stock = new OwnedStock("TEUM", "laimonas");

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            Assert.Equal("TEUM", stock.State.Ticker);
            Assert.Equal("laimonas", stock.State.UserId);
            Assert.Equal(10, stock.State.Owned);
            Assert.Equal(21, stock.State.Spent);

            stock.Purchase(5, 2, DateTime.UtcNow);

            Assert.Equal(15, stock.State.Owned);
            Assert.Equal(31, stock.State.Spent);
            Assert.Equal(0, stock.State.Profit);

            stock.Sell(5, 20, DateTime.UtcNow);

            Assert.Equal(10, stock.State.Owned);
            Assert.Equal(100, stock.State.Earned);
            Assert.NotNull(stock.State.Sold);
            Assert.Equal(69, stock.State.Profit);
        }

        [Fact]
        public void SellingNotOwnedFails()
        {
            var stock = new OwnedStock("TEUM", "laimonas");

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            Assert.ThrowsAny<Exception>(() => stock.Sell(20, 100, DateTime.UtcNow));
        }

        [Fact]
        public void BuyingForZeroThrows()
        {
            var stock = new OwnedStock("ticker", "user");

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(10, 0, DateTime.UtcNow));
        }

        [Fact]
        public void BuyingWithBadDateThrows()
        {
            var stock = new OwnedStock("ticker", "user");

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(10, 0, DateTime.MinValue));
        }

        [Fact]
        public void BuyingWithBadTickerThrows()
        {
            Assert.Throws<InvalidOperationException>(() => new OwnedStock("", "user"));
        }

        [Fact]
        public void BuyingWithBadUserThrows()
        {
            Assert.Throws<InvalidOperationException>(() => new OwnedStock("ticker", ""));
        }

        [Fact]
        public void PurchaseWithDateNotProvidedThrows()
        {
            var stock = new OwnedStock("ticker", "userid");

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(1, 20, DateTime.MinValue));
        }

        [Fact]
        public void EventCstrReplaysEvents()
        {
            var stock = new OwnedStock("ticker", "userid");

            stock.Purchase(1, 10, DateTime.UtcNow);

            var events = stock.Events;

            var stock2 = new OwnedStock(events);

            Assert.Equal(stock.State.Owned, stock2.State.Owned);
            Assert.Equal(stock.State.Purchased, stock2.State.Purchased);
        }
    }
}
