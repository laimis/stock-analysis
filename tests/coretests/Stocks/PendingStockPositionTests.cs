using System;
using System.Linq;
using core.Stocks;
using testutils;
using Xunit;

namespace coretests.Stocks
{
    public class PendingStockPositionTests
    {
        [Fact]
        public void Create_AppliesProperties()
        {
            var pending = CreateTestPendingPosition();
            
            Assert.Equal("this is a note", pending.State.Notes);
            Assert.Equal(100, pending.State.NumberOfShares);
            Assert.Equal(10, pending.State.Bid);
            Assert.Equal(5, pending.State.StopPrice);
            Assert.Equal("alltimehigh", pending.State.Strategy);
            Assert.Equal(TestDataGenerator.AMD, pending.State.Ticker);
            Assert.Equal(-500, pending.State.StopLossAmount);
            Assert.NotEqual(Guid.Empty, pending.State.UserId);
        }

        private static PendingStockPosition CreateTestPendingPosition()
        {
            var pending = new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid());
            return pending;
        }

        [Fact]
        public void Create_WithInvalidPrice_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 0,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid()));
        }

        [Fact]
        public void Create_WithInvalidNumberOfShares_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 0,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid()));
        }
        
        [Fact]
        public void Create_WithNegativeNumberOfSharesForShortPositions_Works()
        {
            var position = new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: -10,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid());
            
            Assert.Equal(-10, position.State.NumberOfShares);
        }

        [Fact]
        public void Create_WithInvalidStopPrice_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: -1,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid()));
        }

        [Fact]
        public void Create_WithInvalidNotes_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid()));
        }

        [Fact]
        public void Create_WithInvalidUserId_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.Empty));
        }
        
        [Fact]
        public void Create_WithInvalidStrategy_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid()));
        }

        [Fact]
        public void Close_SetClosedDate()
        {
            var pending = CreateTestPendingPosition();

            pending.Close("provided reason");

            Assert.NotNull(pending.State.Closed);
            Assert.Equal("provided reason", pending.State.CloseReason);
        }

        [Fact]
        public void Close_Repeatedly_NoOp()
        {
            var pending = CreateTestPendingPosition();
            
            pending.Close("asdas");
            
            var closed = pending.State.Closed;
            var eventCount = pending.Events.Count();
            
            pending.Close("asdasd");
            
            Assert.Equal(closed, pending.State.Closed);
            Assert.Equal(eventCount, pending.Events.Count());
        }

        [Fact]
        public void MissingCloseReason_Throws()
        {
            var pending = CreateTestPendingPosition();
            Assert.Throws<InvalidOperationException>(() => pending.Close(""));
        }

        [Fact]
        public void Purchase_Works()
        {
            var pending = CreateTestPendingPosition();
            
            pending.Purchase(pending.State.Bid + 1);
            
            Assert.Equal(pending.State.Bid + 1, pending.State.Price);
            Assert.NotNull(pending.State.Closed);
            Assert.True(pending.State.Purchased);
        }

        [Fact]
        public void AddOrderDetails_Works()
        {
            var pending = CreateTestPendingPosition();
            
            pending.AddOrderDetails("limit", "gtc");
            
            Assert.Equal("limit", pending.State.OrderType);
            Assert.Equal("gtc", pending.State.OrderDuration);
        }
    }
}
