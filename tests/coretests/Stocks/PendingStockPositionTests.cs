using System;
using core.Stocks;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks
{
    public class PendingStockPositionTests
    {
        [Fact]
        public void Create_AppliesProperties()
        {
            var pending = new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid());
            
            Assert.Equal("this is a note", pending.State.Notes);
            Assert.Equal(100, pending.State.NumberOfShares);
            Assert.Equal(10, pending.State.Bid);
            Assert.Equal(5, pending.State.StopPrice);
            Assert.Equal("alltimehigh", pending.State.Strategy);
            Assert.Equal(TestDataGenerator.AMD, pending.State.Ticker);
            Assert.NotEqual(Guid.Empty, pending.State.UserId);
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
            var pending = new PendingStockPosition(
                notes: "this is a note",
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                strategy: "alltimehigh",
                ticker: TestDataGenerator.AMD,
                userId: Guid.NewGuid());

            pending.Close();

            Assert.NotNull(pending.State.Closed);
        }
    }
}