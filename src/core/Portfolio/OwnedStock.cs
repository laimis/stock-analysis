using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Portfolio
{
    public class OwnedStock : Aggregate
    {
        public OwnedStockState State { get; }

        public OwnedStock() : base()
        {
            this.State = new OwnedStockState();
        }

        public OwnedStock(List<AggregateEvent> events) : base(events)
        {
            this.State = new OwnedStockState();
        }

        public OwnedStock(string ticker, string userId) : this()
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