using System;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class OwnedStockTests
    {
        private static Guid _guid = Guid.NewGuid();
            
        [Fact]
        public void PurchaseWorks()
        {
            var stock = new OwnedStock("TEUM", _guid);

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            Assert.Equal("TEUM", stock.State.Ticker);
            Assert.Equal(_guid, stock.State.UserId);
            Assert.Equal(10, stock.State.Owned);
            Assert.Equal(21, stock.State.Spent);

            stock.Purchase(5, 2, DateTime.UtcNow);

            Assert.Equal(15, stock.State.Owned);
            Assert.Equal(31, stock.State.Spent);

            stock.Sell(5, 20, DateTime.UtcNow, "sample note");

            Assert.Equal(10, stock.State.Owned);
            Assert.NotNull(stock.State.Sold);
        }

        [Fact]
        public void SellingNotOwnedFails()
        {
            var stock = new OwnedStock("TEUM", _guid);

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            Assert.ThrowsAny<Exception>(() => stock.Sell(20, 100, DateTime.UtcNow, "sample note"));
        }

        [Fact]
        public void BuyingForZeroThrows()
        {
            var stock = new OwnedStock("tlsa", _guid);

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(10, 0, DateTime.UtcNow));
        }

        [Fact]
        public void BuyingWithBadDateThrows()
        {
            var stock = new OwnedStock("tlsa", _guid);

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
            var stock = new OwnedStock("tlsa", _guid);

            Assert.Throws<InvalidOperationException>(() => stock.Purchase(1, 20, DateTime.MinValue));
        }

        [Fact]
        public void EventCstrReplaysEvents()
        {
            var stock = new OwnedStock("tlsa", _guid);

            stock.Purchase(1, 10, DateTime.UtcNow);

            var events = stock.Events;

            var stock2 = new OwnedStock(events);

            Assert.Equal(stock.State.Owned, stock2.State.Owned);
            Assert.Equal(stock.State.Purchased, stock2.State.Purchased);
        }
    }
}
