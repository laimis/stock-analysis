using core.Portfolio;
using Xunit;

namespace coretests
{
	public class OwnedStockTests
	{
		[Fact]
		public void PurchaseWorks()
		{
			var stock = new OwnedStock("TEUM", "laimonas", 10, 2.1);

			Assert.Equal("TEUM", stock.State.Ticker);
			Assert.Equal("laimonas", stock.State.UserId);
			Assert.Equal(10, stock.State.Owned);
			Assert.Equal(21, stock.State.Spent);

			stock.Purchase(5, 2);

			Assert.Equal(15, stock.State.Owned);
			Assert.Equal(31, stock.State.Spent);
		}
	}
}
