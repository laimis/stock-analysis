using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate
    {
        private OwnedStockState _state;
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
        public string Description => this.State.Description;

        public double AverageCost => this.State.AverageCost;

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

        public void DeleteTransaction(Guid transactionId)
        {
            if (!this.State.BuyOrSell.Any(t => t.Id == transactionId))
            {
                return;
            }

            Apply(
                new StockTransactionDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    transactionId,
                    DateTimeOffset.UtcNow
                    
                )
            );
        }

        internal void Delete()
        {
            Apply(
                new StockDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow
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

        private void ApplyInternal(TickerObtained o)
        {
            _state = new OwnedStockState(
                o.AggregateId,
                o.Ticker,
                o.UserId
            );
        }

        private void ApplyInternal(StockSold sold)
        {
            this.State.Apply(sold);
        }

        private void ApplyInternal(StockDeleted deleted)
        {
            this.State.Apply(deleted);
        }

        private void ApplyInternal(StockTransactionDeleted deleted)
        {
            this.State.Apply(deleted);
        }
    }
}