using System;
using core.Shared;
using core.Stocks;

namespace core.Stocks
{
    public class PendingStockPositionState : IAggregateState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Bid { get; private set; }
        public decimal? Price { get; private set;}
        public decimal? PercentDiffBetweenBidAndPrice { get; private set; }
        public decimal NumberOfShares { get; private set; }
        public decimal? StopPrice { get; private set; }
        public string Notes { get; private set; }
        public string Strategy { get; private set; }
        public DateTimeOffset Opened { get; private set; }
        public DateTimeOffset? Closed { get; private set; }
        public bool IsClosed => Closed.HasValue;
        public bool Purchased { get; private set; }

        public void Apply(AggregateEvent e) => ApplyInternal(e);

        protected void ApplyInternal(dynamic obj) => ApplyInternal(obj);

        public PendingStockPositionState SetPrice(decimal price)
        {
            Price = price;
            PercentDiffBetweenBidAndPrice = (price - Bid) / Bid;
            return this;
        }

        private void ApplyInternal(PendingStockPositionCreated created)
        {
            ApplyInternal(
                new PendingStockPositionCreatedWithStrategy(
                    id: created.Id,
                    aggregateId: created.AggregateId,
                    when: created.When,
                    notes: created.Notes,
                    numberOfShares: created.NumberOfShares,
                    price: created.Price,
                    stopPrice: created.StopPrice,
                    strategy: null,
                    ticker: created.Ticker,
                    userId: created.UserId
                )
            );
        }

        private void ApplyInternal(PendingStockPositionCreatedWithStrategy created)
        {
            Opened = created.When;
            Id = created.AggregateId;
            Notes = created.Notes;
            NumberOfShares = created.NumberOfShares;
            Bid = created.Price;
            StopPrice = created.StopPrice;
            Strategy = created.Strategy;
            Ticker = new Ticker(created.Ticker);
            UserId = created.UserId;
        }

        private void ApplyInternal(PendingStockPositionClosed closed)
        {
            Closed = closed.When;
            Purchased = closed.Purchased;
        }
    }
}
