using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStock : Aggregate<OwnedStockState>
    {
        public OwnedStock(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedStock(Ticker ticker, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            Apply(new TickerObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, ticker.Value, userId));
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
                    State.Ticker.Value,
                    numberOfShares,
                    price,
                    notes,
                    stopPrice
                )
            );
        }

        public void DeleteStop()
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
                    State.Ticker.Value
                )
            );
        }

        public void SetRiskAmount(decimal riskAmount, int positionId)
        {
            var position = State.GetPosition(positionId);
            if (position == null)
            {
                throw new InvalidOperationException("Unable to find position with id " + positionId);
            }

            if (riskAmount < 0)
            {
                throw new InvalidOperationException("Risk amount cannot be negative");
            }

            Apply(new PositionRiskAmountSet(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, State.UserId, positionId, riskAmount));
        }

        public bool SetStop(decimal stopPrice)
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("No open position to set stop on");
            }

            if (stopPrice < 0)
            {
                throw new InvalidOperationException("Stop price cannot be negative");
            }

            if (stopPrice == State.OpenPosition.StopPrice)
            {
                return false;
            }

            Apply(
                new StopPriceSet(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, State.UserId, State.Ticker.Value, stopPrice)
            );

            return true;
        }

        public bool AddNotes(string notes)
        {
            if (State.OpenPosition == null)
            {
                throw new InvalidOperationException("No open position to add notes to");
            }

            if (string.IsNullOrEmpty(notes))
            {
                return false;
            }

            if (State.OpenPosition.Notes.Contains(notes))
            {
                return false;
            }

            Apply(
                new NotesAdded(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    State.UserId,
                    State.OpenPosition.PositionId,
                    notes
                )
            );

            return true;
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
                    State.Ticker.Value,
                    numberOfShares,
                    price,
                    notes)
            );
        }

        public bool AssignGrade(int positionId, TradeGrade grade, string note)
        {
            var position = State.GetPosition(positionId);
            if (position == null)
            {
                throw new InvalidOperationException("Unable to find position with id " + positionId);
            }

            if (!position.IsClosed)
            {
                throw new InvalidOperationException("Cannot assign grade to an open position");
            }

            if (position.Grade.Equals(grade) && position.GradeNote == note)
            {
                return false;
            }

            Apply(
                new TradeGradeAssigned(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    userId: State.UserId,
                    grade: grade.Value,
                    note: note,
                    positionId: positionId
                )
            );

            return true;
        }

        public bool DeletePosition(int positionId)
        {
            var position = State.GetPosition(positionId);
            if (position == null) // already deleted before, moving on
            {
                return false;
            }

            // we don't want to mess with closed positions, at leats for now
            if (position.IsClosed)
            {
                throw new InvalidOperationException("Cannot delete a closed position");
            }

            Apply(
                new PositionDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    State.UserId,
                    positionId
                )
            );

            return true;
        }

        public bool SetPositionLabel(int positionId, string key, string value)
        {
            var position = State.GetPosition(positionId);
            if (position == null)
            {
                throw new InvalidOperationException("Unable to find position with id " + positionId);
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Key cannot be empty");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException("Value cannot be empty");
            }

            if (position.ContainsLabel(key, value))
            {
                return false;
            }

            Apply(
                new PositionLabelSet(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    State.UserId,
                    positionId,
                    key,
                    value
                )
            );

            return true;
        }

        public bool DeletePositionLabel(int positionId, string key)
        {
            var position = State.GetPosition(positionId);
            if (position == null)
            {
                throw new InvalidOperationException("Unable to find position with id " + positionId);
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Key cannot be empty");
            }

            if (!position.ContainsLabel(key))
            {
                return false;
            }

            Apply(
                new PositionLabelDeleted(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    State.UserId,
                    positionId,
                    key
                )
            );

            return true;
        }
    }
}