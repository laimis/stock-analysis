using System;
using System.Collections.Generic;

namespace core.Portfolio
{
	public class OwnedStock
	{
		public OwnedStockState State { get; }
		private List<AggregateEvent> _events { get; }
		public int Version { get; }

		public IReadOnlyList<AggregateEvent> Events => _events.AsReadOnly();

		public OwnedStock()
		{
			this.State = new OwnedStockState();
			this._events = new List<AggregateEvent>();
		}

		public OwnedStock(List<AggregateEvent> events) : this()
		{
			foreach(var e in events)
			{
				Apply(e);
			}

			this.Version = events.Count;
		}

		public OwnedStock(string ticker, string userId) : this()
		{
			Apply(new TickerObtained(ticker, userId, DateTime.UtcNow));
		}

		public void Purchase(int amount, double price, DateTime date)
		{
			if (price <= 0)
			{
				throw new InvalidOperationException("Price cannot be empty or zero");
			}

			if (date == DateTime.MinValue)
			{
				throw new InvalidOperationException("Purchase date not specified");
			}
			
			Apply(new StockPurchased(this.State.Ticker, this.State.UserId, amount, price, date));
		}

		public void Sell(int amount, double price, DateTime date)
		{
			if (amount > this.State.Owned)
			{
				throw new InvalidOperationException("Amount owned is less than what is desired to sell");
			}
			
			Apply(new StockSold(this.State.Ticker, this.State.UserId, amount, price, date));
		}

		private void Apply(AggregateEvent obj)
		{
			this._events.Add(obj);

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

		private void ApplyInternal(StockSold sold)
		{
			this.State.Owned -= sold.Amount;
			this.State.Earned += sold.Amount * sold.Price;
		}
	}
}