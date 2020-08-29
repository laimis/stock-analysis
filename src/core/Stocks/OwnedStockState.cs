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

        public int Owned { get; private set; }
        public double Cost { get; private set; }
        public double AverageCost { get; private set; }
        public List<Transaction> Transactions { get; } = new List<Transaction>();
        internal List<IStockTransaction> BuyOrSell { get; } = new List<IStockTransaction>();
        internal HashSet<Guid> Deletes { get; } = new HashSet<Guid>();

        public string Description => $"{this.Owned} shares owned at avg cost {Math.Round(this.AverageCost, 2)}";

        public string Category { get; private set; }
        public int DaysHeld { get; private set; }

        internal void ApplyInternal(TickerObtained o)
        {
            this.Id = o.AggregateId;
            this.Ticker = o.Ticker;
            this.UserId = o.UserId;
        }

        internal void ApplyInternal(StockCategoryChanged c)
        {
            this.Category = c.Category;
        }

        internal void ApplyInternal(StockPurchased purchased)
        {
            this.BuyOrSell.Add(purchased);

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockDeleted deleted)
        {
            foreach(var t in this.BuyOrSell)
            {
                this.Deletes.Add(t.Id);
            }

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockTransactionDeleted deleted)
        {
            this.Deletes.Add(deleted.TransactionId);

            StateUpdateLoop();
        }

        internal void ApplyInternal(StockSold sold)
        {
            this.BuyOrSell.Add(sold);

            StateUpdateLoop();
        }

        private void StateUpdateLoop()
        {
            double avgCost = 0;
            int owned = 0;
            double cost = 0;
            var txs = new List<Transaction>();
            DateTimeOffset? mostRecentOpen = null;

            void PurchaseProcessing(IStockTransaction st)
            {
                avgCost = (avgCost * owned + st.Price * st.NumberOfShares)
                        / (owned + st.NumberOfShares);
                owned += st.NumberOfShares;
                cost += st.Price * st.NumberOfShares;

                txs.Add(
                    Transaction.DebitTx(
                        this.Id,
                        st.Id,
                        this.Ticker,
                        $"Purchased {st.NumberOfShares} shares @ ${st.Price}/share",
                        st.Price * st.NumberOfShares,
                        st.When,
                        false
                    )
                );
            }

            void SellProcessing(IStockTransaction st)
            {
                txs.Add(
                    Transaction.CreditTx(
                        this.Id,
                        st.Id,
                        this.Ticker,
                        $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                        st.Price * st.NumberOfShares,
                        st.When,
                        false
                    )
                );

                txs.Add(
                    Transaction.PLTx(
                        this.Id,
                        this.Ticker,
                        $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                        avgCost * st.NumberOfShares,
                        st.Price * st.NumberOfShares,
                        st.When,
                        false
                    )
                );
                
                owned -= st.NumberOfShares;
                cost -= avgCost * st.NumberOfShares;
            }

            foreach(var st in this.BuyOrSell.OrderBy(e => e.When).ThenBy(i => this.BuyOrSell.IndexOf(i)))
            {
                if (this.Deletes.Contains(st.Id))
                {
                    continue;
                }

                if (st is StockPurchased)
                {
                    if (owned == 0)
                    {
                        mostRecentOpen = st.When;
                    }

                    PurchaseProcessing(st);
                }
                else
                {
                    SellProcessing(st);
                }

                if (owned == 0)
                {
                    avgCost = 0;
                    cost = 0;
                    mostRecentOpen = null;
                }
            }

            this.AverageCost = avgCost;
            this.Owned = owned;
            this.Cost = cost;
            this.Transactions.Clear();
            this.Transactions.AddRange(txs);
            this.DaysHeld = mostRecentOpen.HasValue ? (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(mostRecentOpen.Value).TotalDays)
                : 0;
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }
    }

    internal interface IStockTransaction
    {
        int NumberOfShares { get; }
        double Price { get; }
        Guid Id { get; }
        DateTimeOffset When { get; }
    }
}