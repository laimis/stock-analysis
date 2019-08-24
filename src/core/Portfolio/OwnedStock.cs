using System;
using System.Collections.Generic;

namespace core.Portfolio
{
	public class OwnedStock
	{
		public OwnedStockState State { get; }
		private List<AggregateEvent> Events { get; }

		public OwnedStock()
		{
			this.State = new OwnedStockState();
			this.Events = new List<AggregateEvent>();
		}

		public OwnedStock(string ticker, string userId, int amount, double price) : this()
		{
			Apply(new TickerObtained(ticker, userId, DateTime.UtcNow));
			
			Purchase(amount, price);
		}

		public void Purchase(int amount, double price)
		{
			Apply(new StockPurchased(this.State.Ticker, this.State.UserId, amount, price, DateTime.UtcNow));
		}

		private void Apply(AggregateEvent obj)
		{
			this.Events.Add(obj);

			ApplyInternal(obj);
		}

		private void ApplyInternal(dynamic obj)
		{
			this.ApplyInternal(obj);
		}

		private void ApplyInternal(StockPurchased purchased)
		{
			this.State.Owned += purchased.Amount;
			this.State.Spent += purchased.Amount  * purchased.Price;
		}

		private void ApplyInternal(TickerObtained tickerObtained)
		{
			this.State.Ticker = tickerObtained.Ticker;
			this.State.UserId = tickerObtained.UserId;
		}
	}
}