using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate
    {
        private OwnedStockState _state = new OwnedStockState();
        public OwnedStockState State => _state;
        public override Guid Id => State.Id;

        public OwnedStock(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedStock(Ticker ticker, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(new TickerObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, ticker, userId));
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

        public string Ticker => this.State.Ticker;
        public string Description => $"{this.State.Owned} shares owned";

        public void Purchase(int numberOfShares, double price, DateTimeOffset date, string notes = null)
        {
            if (price <= 0)
            {
                throw new InvalidOperationException("Price cannot be empty or zero");
            }

            if (date == DateTime.MinValue)
            {
                throw new InvalidOperationException("Purchase date not specified");
            }

            Apply(
                new StockPurchased(
                    Guid.NewGuid(),
                    this.State.Id,
                    date,
                    this.State.UserId,
                    this.State.Ticker,
                    numberOfShares,
                    price,
                    notes
                )
            );
        }

        public void Sell(int numberOfShares, double price, DateTimeOffset date, string notes)
        {
            if (numberOfShares > this.State.Owned)
            {
                throw new InvalidOperationException("Number of shares owned is less than what is desired to sell");
            }

            Apply(
                new StockSold(
                    Guid.NewGuid(),
                    this.State.Id,
                    date,
                    this.State.UserId,
                    this.State.Ticker,
                    numberOfShares,
                    price,
                    notes)
            );
        }

        private void ApplyInternal(StockPurchased purchased)
        {
            this.State.Apply(purchased);
        }

        private void ApplyInternal(TickerObtained tickerObtained)
        {
            this.State.Id = tickerObtained.AggregateId;
            this.State.Ticker = tickerObtained.Ticker;
            this.State.UserId = tickerObtained.UserId;
        }

        private void ApplyInternal(StockSold sold)
        {
            this.State.Apply(sold);
        }
    }
}