using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate
    {
        public OwnedStockState State { get; } = new OwnedStockState();
        public override IAggregateState AggregateState => State;
        
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

        public void Purchase(decimal numberOfShares, decimal price, DateTimeOffset date, string notes = null, decimal? stopPrice = null)
        {
            if (price <= 0)
            {
                throw new InvalidOperationException("Price cannot be negative or zero");
            }

            if (date == DateTime.MinValue)
            {
                throw new InvalidOperationException("Purchase date not specified");
            }

            Apply(
                new StockPurchased_v2(
                    Guid.NewGuid(),
                    State.Id,
                    date,
                    State.UserId,
                    State.Ticker,
                    numberOfShares,
                    price,
                    notes,
                    stopPrice
                )
            );
        }

        internal void DeleteStop()
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("No open position to delete stop for");
            }

            Apply(
                new StopDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    State.UserId,
                    State.Ticker
                )
            );
        }

        internal void SetRiskAmount(decimal riskAmount)
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("Cannot set risk amount on a stock that has no open position");
            }

            if (riskAmount < 0)
            {
                throw new InvalidOperationException("Risk amount cannot be negative");
            }

            Apply(new RiskAmountSet(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, State.UserId, State.Ticker, riskAmount));
        }

        internal void SetStop(decimal stopPrice)
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("No open position to set stop on");
            }

            if (stopPrice < 0)
            {
                throw new InvalidOperationException("Stop price cannot be negative");
            }

            Apply(
                new StopPriceSet(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, State.UserId, State.Ticker, stopPrice)
            );
        }

        public void UpdateSettings(string category)
        {
            if (string.IsNullOrEmpty(category))
            {
                return;
            }
            
            if (State.OpenPosition.Category == category)
            {
                return;
            }

            if (!StockCategory.IsValid(category))
            {
                throw new InvalidOperationException(
                    $"Invalid stock category: {category}, only {string.Join(", ", StockCategory.All)} are valid"
                );
            }

            Apply(
                new StockCategoryChanged(
                    Guid.NewGuid(),
                    State.Id,
                    category,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public void DeleteTransaction(Guid transactionId)
        {
            if (!State.BuyOrSell.Any(t => t.Id == transactionId))
            {
                throw new InvalidOperationException("Unable to find transcation to delete using id " + transactionId);
            }

            Apply(
                new StockTransactionDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    transactionId,
                    DateTimeOffset.UtcNow
                    
                )
            );
        }

        public void Delete()
        {
            Apply(
                new StockDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public void Sell(decimal numberOfShares, decimal price, DateTimeOffset date, string notes)
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("No open position to sell");
            }
            
            if (State.OpenPosition.NumberOfShares < numberOfShares)
            {
                throw new InvalidOperationException("Cannot sell more shares than owned");
            }

            if (price < 0)
            {
                throw new InvalidOperationException("Price cannot be negative or zero");
            }

            Apply(
                new StockSold(
                    Guid.NewGuid(),
                    State.Id,
                    date,
                    State.UserId,
                    State.Ticker,
                    numberOfShares,
                    price,
                    notes)
            );
        }
    }
}