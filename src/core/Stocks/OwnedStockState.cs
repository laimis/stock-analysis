using System;
using System.Collections.Generic;
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

        internal void ApplyInternal(TickerObtained o)
        {
            this.Id = o.AggregateId;
            this.Ticker = o.Ticker;
            this.UserId = o.UserId;
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

            foreach(var st in this.BuyOrSell)
            {
                if (this.Deletes.Contains(st.Id))
                {
                    continue;
                }

                if (st is StockPurchased)
                {
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
                }
            }

            this.AverageCost = avgCost;
            this.Owned = owned;
            this.Cost = cost;
            this.Transactions.Clear();
            this.Transactions.AddRange(txs);
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
}