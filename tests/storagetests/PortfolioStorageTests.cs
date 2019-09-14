using System;
using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;
using storage;
using Xunit;

namespace storagetests
{
    public class PortfolioStorageTests
    {
        const string _userId = "testuser";

        [Fact]
        public async Task OwnedStockStorageAsync()
        {
            var stock = new OwnedStock(GenerateTestTicker(), _userId);

            stock.Purchase(10, 2.1, DateTime.UtcNow);

            var storage = new PortfolioStorage(StorageTests._cnn);

            await storage.Save(stock);

            var loadedList = await storage.GetStocks(_userId);

            Assert.NotEmpty(loadedList);

            var loaded = await storage.GetStock(stock.State.Ticker, _userId);

            Assert.Equal(loaded.State.Owned, stock.State.Owned);

            loaded.Purchase(5, 5, DateTime.UtcNow);

            await storage.Save(loaded);

            loaded = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

            Assert.NotEqual(loaded.State.Owned, stock.State.Owned);
        }

        private static string GenerateTestTicker()
        {
            return $"test-{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        }

        [Fact]
        public async Task OwnedOption_WorksAsync()
        {
            var option = new SoldOption(
                GenerateTestTicker(),
                OptionType.CALL,
                new DateTimeOffset(2019, 9, 20, 0, 0, 0, 0, TimeSpan.FromSeconds(0)),
                2.5,
                _userId
            );

            option.Open(1, 8, DateTimeOffset.UtcNow);

            var storage = new PortfolioStorage(StorageTests._cnn);

            await storage.Save(option);

            var loaded = await storage.GetSoldOption(
                option.State.Ticker,
                option.State.Type,
                option.State.Expiration,
                option.State.StrikePrice,
                _userId);

            Assert.NotNull(loaded);

            Assert.Equal(option.State.StrikePrice, loaded.State.StrikePrice);

            var list = await storage.GetSoldOptions(_userId);

            var fromList = list.Single(o => o.State.Ticker == option.State.Ticker);

            Assert.Equal(option.State.StrikePrice, fromList.State.StrikePrice);
        }
    }
}