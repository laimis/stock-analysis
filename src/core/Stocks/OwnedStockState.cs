using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStockState
	{
        public OwnedStockState(Guid id, string ticker, Guid userId)
        {
            this.Id = id;
            this.Ticker = ticker;
            this.UserId = userId;
        }

        public Guid Id { get; }
        public string Ticker { get; }
        public Guid UserId { get; }

        public int Owned { get; private set; }
        public double Cost { get; private set; }
        public double AverageCost { get; private set; }
        public List<Transaction> Transactions { get; } = new List<Transaction>();
        internal List<IStockTransaction> BuyOrSell { get; } = new List<IStockTransaction>();
        internal HashSet<Guid> Deletes { get; } = new HashSet<Guid>();

        public string Description => $"{this.Owned} shares owned at avg cost {Math.Round(this.AverageCost, 2)}";

        internal void Apply(StockPurchased purchased)
        {
            this.BuyOrSell.Add(purchased);

            StateUpdateLoop();
        }

        internal void Apply(StockDeleted deleted)
        {
            foreach(var t in this.BuyOrSell)
            {
                this.Deletes.Add(t.Id);
            }

            StateUpdateLoop();
        }

        internal void Apply(StockTransactionDeleted deleted)
        {
            this.Deletes.Add(deleted.TransactionId);

            StateUpdateLoop();
        }

        internal void Apply(StockSold sold)
        {
            this.BuyOrSell.Add(sold);

            StateUpdateLoop();
        }

        private void StateUpdateLoop()
        {
            double avgCost = 0;
            int owned = 0;
            double cost = 0;

            void PurchaseProcessing(IStockTransaction st)
            {
                avgCost = (avgCost * owned + st.Price * st.NumberOfShares)
                        / (owned + st.NumberOfShares);
                owned += st.NumberOfShares;
                cost += st.Price * st.NumberOfShares;

                this.Transactions.Add(Transaction.DebitTx(
                    this.Id,
                    st.Id,
                    this.Ticker,
                    $"Purchased {st.NumberOfShares} shares @ ${st.Price}/share",
                    st.Price * st.NumberOfShares,
                    st.When,
                    false
                ));
            }

            void SellProcessing(IStockTransaction st)
            {
                this.Transactions.Add(Transaction.CreditTx(
                    this.Id,
                    st.Id,
                    this.Ticker,
                    $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                    st.Price * st.NumberOfShares,
                    st.When,
                    false
                ));

                this.Transactions.Add(Transaction.PLTx(
                    this.Id,
                    this.Ticker,
                    $"Sold {st.NumberOfShares} shares @ ${st.Price}/share",
                    avgCost * st.NumberOfShares,
                    st.Price * st.NumberOfShares,
                    st.When,
                    false
                ));
                
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
        }
    }
}