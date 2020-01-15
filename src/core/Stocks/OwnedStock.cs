using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate
    {
        private OwnedStockState _state = new OwnedStockState();
        public OwnedStockState State => _state;

        public OwnedStock(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedStock(string ticker, string userId)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new InvalidOperationException("Missing ticker value");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(new TickerObtained(ticker, userId, DateTime.UtcNow));
        }

        protected override void Apply(AggregateEvent obj)
        {
            this._events.Add(obj);

            ApplyInternal(obj);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
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

        private void ApplyInternal(StockPurchased purchased)
        {
            this.State.Owned += purchased.Amount;
            this.State.Spent += purchased.Amount * purchased.Price;
            this.State.Purchased = purchased.When;
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
            this.State.Sold = sold.When;
        }
    }
}