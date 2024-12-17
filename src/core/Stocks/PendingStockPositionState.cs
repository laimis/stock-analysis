using System;
using core.Shared;
using Microsoft.FSharp.Core;

namespace core.Stocks
{
    public class PendingStockPositionState : IAggregateState
    {
        public Guid Id { get; private set; }
        public Ticker Ticker { get; private set; }
        public Guid UserId { get; private set; }
        public decimal Bid { get; private set; }
        public decimal? Price { get; private set;}
        // public decimal? PercentDiffBetweenBidAndPrice { get; private set; }
        public decimal NumberOfShares { get; private set; }
        public FSharpOption<decimal> StopPrice { get; private set; }
        public FSharpOption<decimal> SizeStopPrice { get; private set; }
        public string Notes { get; private set; }
        public string Strategy { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public FSharpOption<DateTimeOffset> Closed { get; private set; }
        public bool IsClosed => FSharpOption<DateTimeOffset>.get_IsSome(Closed);
        public bool HasStopPrice => FSharpOption<decimal>.get_IsSome(StopPrice);
        public bool IsOpen => !IsClosed;
        public bool Purchased { get; private set; }
        public string CloseReason { get; private set; }
        public int NumberOfDaysActive => (int)((IsClosed ? Closed.Value : DateTimeOffset.UtcNow) - Created).TotalDays;
        public decimal StopLossAmount => HasStopPrice ? NumberOfShares * (StopPrice.Value - Bid) : 0;
        public string OrderDuration { get; private set; }
        public string OrderType { get; private set; }

        public void Apply(AggregateEvent e) => ApplyInternal(e);

        private void ApplyInternal(dynamic obj) => ApplyInternal(obj);

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
            ApplyInternal(
                new PendingStockPositionCreatedWithStrategyAndSizeStop(
                    id: created.Id,
                    aggregateId: created.AggregateId,
                    when: created.When,
                    notes: created.Notes,
                    numberOfShares: created.NumberOfShares,
                    price: created.Price,
                    stopPrice: created.StopPrice,
                    sizeStopPrice: null,
                    strategy: created.Strategy,
                    ticker: created.Ticker,
                    userId: created.UserId
                )
            );
        }

        private void ApplyInternal(PendingStockPositionCreatedWithStrategyAndSizeStop created)
        {
            Created = created.When;
            Id = created.AggregateId;
            Notes = created.Notes;
            NumberOfShares = created.NumberOfShares;
            Bid = created.Price;
            StopPrice = created.StopPrice;
            SizeStopPrice = created.SizeStopPrice;
            Strategy = created.Strategy;
            Ticker = new Ticker(created.Ticker);
            UserId = created.UserId;
        }
        
        private void ApplyInternal(PendingStockPositionOrderDetailsAdded details)
        {
            OrderType = details.OrderType;
            OrderDuration = details.OrderDuration;
        }

        private void ApplyInternal(PendingStockPositionClosed closed)
        {
            // closed should not be used anymore, and we are changing the event either to closedwithreason or realized
            if (closed.Price.HasValue)
            {
                ApplyInternal(new PendingStockPositionRealized(closed.Id, closed.AggregateId, closed.When, closed.Price.Value));
            }
            else
            {
                ApplyInternal(new PendingStockPositionClosedWithReason(closed.Id, closed.AggregateId, closed.When, "Reason not provided"));
            }
        }
        
        
        private void ApplyInternal(PendingStockPositionClosedWithReason closed)
        {
            Closed = closed.When;
            CloseReason = closed.Reason;
        }

        private void ApplyInternal(PendingStockPositionRealized realized)
        {
            Price = realized.Price;
            Purchased = true;
            Closed = realized.When;
        }
    }
}
