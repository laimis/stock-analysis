using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStockState : IAggregateState
	{
        public Guid Id { get; private set; }
        public string Ticker { get; private set; }
        public Guid UserId { get; private set; }

        public List<Transaction> Transactions { get; } = new List<Transaction>();
        internal List<IStockTransaction> BuyOrSell { get; } = new List<IStockTransaction>();
        
        private List<PositionInstance> Positions { get; } = new List<PositionInstance>();
        public PositionInstance OpenPosition { get; private set;}
        internal PositionInstance GetPosition(int positionId) =>
            Positions.SingleOrDefault(x => x.PositionId == positionId);
        internal IEnumerable<PositionInstance> GetClosedPositions() => Positions.Where(x => x.IsClosed);
        internal IReadOnlyList<PositionInstance> GetAllPositions() => Positions.AsReadOnly();

        private int _positionId = 0;

        internal void ApplyInternal(TickerObtained o)
        {
            Id = o.AggregateId;
            Ticker = o.Ticker;
            UserId = o.UserId;
        }

        // ignore obsolete method, we need to support old events
        [Obsolete]
        internal void ApplyInternal(StockCategoryChanged c) {}

        internal void ApplyInternal(StockPurchased purchased)
        {
            ApplyInternal(
                new StockPurchased_v2(purchased.Id, purchased.AggregateId, purchased.When, purchased.UserId, purchased.Ticker, purchased.NumberOfShares, purchased.Price, purchased.Notes, null)
            );
        }

        internal void ApplyInternal(StopPriceSet stopPriceSet)
        {
            OpenPosition.SetStopPrice(stopPriceSet.StopPrice, stopPriceSet.When);
        }

        internal void ApplyInternal(StopDeleted deleted)
        {
            OpenPosition.DeleteStopPrice(deleted.When);
        }

        internal void ApplyInternal(RiskAmountSet riskAmountSet)
        {
            OpenPosition.SetRiskAmount(riskAmountSet.RiskAmount, riskAmountSet.When);
        }

        internal void ApplyInternal(TradeGradeAssigned gradeAssigned)
        {
            var position = Positions.Single(x => x.PositionId == gradeAssigned.PositionId);
            position.SetGrade(gradeAssigned.Grade, gradeAssigned.Note);
        }

        internal void ApplyInternal(PositionRiskAmountSet riskAmountSet)
        {
            var position = Positions.Single(x => x.PositionId == riskAmountSet.PositionId);
            position.SetRiskAmount(riskAmountSet.RiskAmount, riskAmountSet.When);
        }

        internal void ApplyInternal(StockPurchased_v2 purchased)
        {
            BuyOrSell.Add(purchased);

            if (OpenPosition == null)
            {
                OpenPosition = new PositionInstance(_positionId, purchased.Ticker);
                Positions.Add(OpenPosition);
                _positionId++;
            }

            OpenPosition.Buy(numberOfShares: purchased.NumberOfShares, price: purchased.Price, transactionId: purchased.Id, when: purchased.When, notes: purchased.Notes);

            if (purchased.StopPrice.HasValue)
            {
                OpenPosition.SetStopPrice(purchased.StopPrice.Value, purchased.When);
            }

            Transactions.Add(
                Transaction.NonPLTx(
                    Id,
                    purchased.Id,
                    Ticker,
                    $"Purchased {purchased.NumberOfShares} shares @ ${purchased.Price}/share",
                    purchased.Price,
                    -purchased.Price * purchased.NumberOfShares,
                    purchased.When,
                    isOption: false
                )
            );
        }

        internal void ApplyInternal(StockDeleted deleted)
        {
            OpenPosition = null;
            BuyOrSell.Clear();
            Positions.Clear();
            Transactions.Clear();
        }

        internal void ApplyInternal(StockTransactionDeleted deleted)
        {
            if (Positions.Count == 0)
            {
                throw new InvalidOperationException("Cannot delete a transaction from a stock that has no position");
            }

            // only last one should be allowed to be deleted?
            // var last = BuyOrSell.LastOrDefault();
            // if (last == null)
            // {
            //     return;
            // }
            // if (last.Id != deleted.TransactionId)
            // {
            //     throw new InvalidOperationException($"Only the last transaction can be deleted for {Ticker}. Expected {last.Id} but got {deleted.TransactionId}");
            // }

            var last = BuyOrSell.Single(t => t.Id == deleted.TransactionId);
            BuyOrSell.Remove(last);
            
            // var transaction = Transactions.LastOrDefault();
            // if (transaction.EventId != deleted.TransactionId)
            // {
            //     throw new InvalidOperationException($"Only the last transaction can be deleted for {Ticker}. Expected {transaction.EventId} but got {deleted.TransactionId}");
            // }

            var transaction = Transactions.Single(t => t.EventId == deleted.TransactionId);
            Transactions.Remove(transaction);

            var lastPosition = Positions.Last();
            lastPosition.RemoveTransaction(deleted.TransactionId);

            if (lastPosition.NumberOfShares == 0)
            {
                OpenPosition = null;
                Positions.Remove(lastPosition);
            }
            else
            {
                OpenPosition = lastPosition;
            }
        }

        internal void ApplyInternal(PositionDeleted deleted)
        {
            var position = Positions.Single(x => x.PositionId == deleted.PositionId);
            
            // remove all transactions for this position
            var transactionsToRemove = position.Transactions.Select(x => x.transactionId).ToList();
            foreach (var transactionId in transactionsToRemove)
            {
                var transaction = Transactions.Single(x => x.EventId == transactionId);
                Transactions.Remove(transaction);

                var buyOrSell = BuyOrSell.Single(x => x.Id == transactionId);
                BuyOrSell.Remove(buyOrSell);
            }

            Positions.Remove(position);
            if (position == OpenPosition)
            {
                OpenPosition = null;
            }
        }

        internal void ApplyInternal(NotesAdded notesAdded)
        {
            var position = Positions.SingleOrDefault(x => x.PositionId == notesAdded.PositionId);
            if (position != null)
            {
                position.AddNotes(notesAdded.Notes);
            }
        }

        internal void ApplyInternal(StockSold sold)
        {
            BuyOrSell.Add(sold);

            if (OpenPosition == null)
            {
                throw new InvalidOperationException("Cannot sell stock that is not owned");
            }

            var profitBefore = OpenPosition.Profit;

            OpenPosition.Sell(
                numberOfShares: sold.NumberOfShares,
                price: sold.Price,
                transactionId: sold.Id,
                when: sold.When,
                notes: sold.Notes
            );

            var profitAfter = OpenPosition.Profit;

            Transactions.Add(
                Transaction.NonPLTx(
                    Id,
                    sold.Id,
                    Ticker,
                    $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                    sold.Price,
                    sold.Price * sold.NumberOfShares,
                    sold.When,
                    isOption: false
                )
            );

            Transactions.Add(
                Transaction.PLTx(
                    Id,
                    Ticker,
                    $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                    sold.Price,
                    amount: profitAfter - profitBefore,
                    when: sold.When,
                    isOption: false
                )
            );

            if (OpenPosition.NumberOfShares == 0)
            {
                OpenPosition = null;
            }
        }

        private void ApplyInternal(PositionLabelSet labelSet)
        {
            var position = Positions.Single(x => x.PositionId == labelSet.PositionId);
            position.SetLabel(labelSet);
        }

        private void ApplyInternal(PositionLabelDeleted labelDeleted)
        {
            var position = Positions.Single(x => x.PositionId == labelDeleted.PositionId);
            position.DeleteLabel(labelDeleted);
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }
    }

    internal interface IStockTransaction
    {
        decimal NumberOfShares { get; }
        decimal Price { get; }
        Guid Id { get; }
        DateTimeOffset When { get; }
        string Notes { get; }
    }

    internal interface IStockTransactionWithStopPrice : IStockTransaction
    {
        decimal? StopPrice { get; }
    }
}