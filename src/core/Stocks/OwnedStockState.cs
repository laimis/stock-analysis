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
        
        public List<PositionInstance> ClosedPositions { get; } = new List<PositionInstance>();
        public PositionInstance OpenPosition { get; private set;}

        internal void ApplyInternal(TickerObtained o)
        {
            Id = o.AggregateId;
            Ticker = o.Ticker;
            UserId = o.UserId;
        }

        internal void ApplyInternal(StockCategoryChanged c) => OpenPosition.SetCategory(c.Category);

        internal void ApplyInternal(StockPurchased purchased)
        {
            ApplyInternal(
                new StockPurchased_v2(purchased.Id, purchased.AggregateId, purchased.When, purchased.UserId, purchased.Ticker, purchased.NumberOfShares, purchased.Price, purchased.Notes, null)
            );
        }

        internal void ApplyInternal(StopPriceSet stopPriceSet)
        {
            OpenPosition.SetStopPrice(stopPriceSet.StopPrice);
        }

        internal void ApplyInternal(RiskAmountSet riskAmountSet)
        {
            OpenPosition.SetRiskAmount(riskAmountSet.RiskAmount);
        }

        internal void ApplyInternal(StockPurchased_v2 purchased)
        {
            BuyOrSell.Add(purchased);

            if (OpenPosition == null)
            {
                OpenPosition = new PositionInstance(purchased.Ticker);
            }

            OpenPosition.Buy(numberOfShares: purchased.NumberOfShares, price: purchased.Price, transactionId: purchased.Id, when: purchased.When, notes: purchased.Notes);

            if (purchased.StopPrice.HasValue)
            {
                OpenPosition.SetStopPrice(purchased.StopPrice.Value);
            }

            Transactions.Add(
                Transaction.DebitTx(
                    Id,
                    purchased.Id,
                    Ticker,
                    $"Purchased {purchased.NumberOfShares} shares @ ${purchased.Price}/share",
                    purchased.Price,
                    purchased.Price * purchased.NumberOfShares,
                    purchased.When,
                    isOption: false
                )
            );
        }

        internal void ApplyInternal(StockDeleted deleted)
        {
            OpenPosition = null;
            BuyOrSell.Clear();
            ClosedPositions.Clear();
            Transactions.Clear();
        }

        internal void ApplyInternal(StockTransactionDeleted deleted)
        {
            if (OpenPosition == null)
            {
                throw new InvalidOperationException("Cannot delete a transaction from a stock that has no open position");
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

            OpenPosition.RemoveTransaction(deleted.TransactionId);

            if (OpenPosition.NumberOfShares == 0)
            {
                OpenPosition = null;
            }
        }

        internal void ApplyInternal(StockSold sold)
        {
            BuyOrSell.Add(sold);

            if (OpenPosition == null)
            {
                throw new InvalidOperationException("Cannot sell stock that is not owned");
            }

            Transactions.Add(
                Transaction.CreditTx(
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
                    OpenPosition.AverageCostPerShare * sold.NumberOfShares,
                    sold.Price * sold.NumberOfShares,
                    sold.When,
                    isOption: false
                )
            );

            OpenPosition.Sell(
                numberOfShares: sold.NumberOfShares,
                price: sold.Price,
                transactionId: sold.Id,
                when: sold.When,
                notes: sold.Notes
            );

            if (OpenPosition.NumberOfShares == 0)
            {
                ClosedPositions.Add(OpenPosition);
                OpenPosition = null;
            }
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