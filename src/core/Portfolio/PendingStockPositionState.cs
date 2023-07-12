using System;
using core.Shared;

namespace core.Portfolio
{
    public class PendingStockPositionState : IAggregateState
    {
        public Guid Id { get; private set; }
        public string Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Bid { get; private set; }
        public decimal? Price { get; private set;}
        public decimal? PercentDiffBetweenBidAndPrice { get; private set; }
        public decimal NumberOfShares { get; private set; }
        public decimal? StopPrice { get; private set; }
        public string Notes { get; private set; }
        public string Strategy { get; private set; }
        public DateTimeOffset Date { get; private set; }
        

        public void Apply(AggregateEvent e)
        {
             ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }

        internal void SetPrice(decimal price)
        {
            this.Price = price;
            this.PercentDiffBetweenBidAndPrice = (price - this.Bid) / this.Bid;
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
            Date = created.When;
            Id = created.AggregateId;
            Notes = created.Notes;
            NumberOfShares = created.NumberOfShares;
            Bid = created.Price;
            StopPrice = created.StopPrice;
            Strategy = created.Strategy;
            Ticker = created.Ticker;
            UserId = created.UserId;
        }
    }
}