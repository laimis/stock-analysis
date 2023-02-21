using System;
using core.Shared;

namespace core.Portfolio
{
    public class PendingStockPositionState : IAggregateState
    {
        public Guid Id { get; private set; }
        public string Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Price { get; private set; }
        public decimal NumberOfShares { get; private set; }
        public decimal? StopPrice { get; private set; }
        public string Notes { get; private set; }
        public DateTimeOffset Date { get; private set; }

        public void Apply(AggregateEvent e)
        {
             ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }

        private void ApplyInternal(PendingStockPositionCreated created)
        {
            Id = created.Id;
            Ticker = created.Ticker;
            UserId = created.UserId;
            Price = created.Price;
            NumberOfShares = created.NumberOfShares;
            StopPrice = created.StopPrice;
            Notes = created.Notes;
            Date = created.When;
        }
    }
}