using System;

namespace core.Portfolio
{
	internal class StockSold : AggregateEvent
	{
		public StockSold(string ticker, string userId, int amount, double price, DateTime date)
			: base(ticker, userId, date)
		{
			this.Amount = amount;
			this.Price = price;
		}

		public int Amount { get; }
		public double Price { get; }
	}
}