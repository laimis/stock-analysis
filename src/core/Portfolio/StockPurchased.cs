using System;

namespace core.Portfolio
{
	internal class StockPurchased : AggregateEvent
	{
		public StockPurchased(string ticker, string userId, int amount, double price, DateTime when)
			: base(userId, when)
		{
			this.Ticker = ticker;
			this.Amount = amount;
			this.Price = price;
		}

		public string Ticker { get; }
		public int Amount { get; }
		public double Price { get; }
	}
}