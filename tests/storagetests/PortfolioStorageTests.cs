using System;
using System.Threading.Tasks;
using core.Portfolio;
using storage;
using Xunit;

namespace storagetests
{
	public class PortfolioStorageTests
	{
		[Fact]
		public async Task OwnedStockStorageAsync()
		{
			var stock = new OwnedStock(Guid.NewGuid().ToString("N").Substring(0, 4), "laimonas", 10, 10);

			var storage = new PortfolioStorage(StorageTests._cnn);

			await storage.Save(stock);

			var loadedList = await storage.GetStocks("laimonas");

			Assert.NotEmpty(loadedList);

			var loaded = await storage.GetStock(stock.State.Ticker, "laimonas");

			Assert.Equal(loaded.State.Owned, stock.State.Owned);

			loaded.Purchase(5, 5);

			await storage.Save(loaded);

			loaded = await storage.GetStock(loaded.State.Ticker, loaded.State.UserId);

			Assert.NotEqual(loaded.State.Owned, stock.State.Owned);
		}
	}
}