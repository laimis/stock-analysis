using System;
using System.Collections.Generic;
using System.Linq;
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

        public int Owned => 
            this.BuyOrSell.Where(a => a is StockPurchased).Sum(b => b.NumberOfShares) 
            - this.BuyOrSell.Where(a => a is StockSold).Sum(s => s.NumberOfShares);

        public double Cost => this.Owned * this.AverageCost;

        public List<Transaction> Transactions { get; } = new List<Transaction>();
        internal List<IStockTransaction> BuyOrSell { get; } = new List<IStockTransaction>();

        public double AverageCost
        {
            get
            {
                double avgCost = 0;
                int owned = 0;

                foreach(var st in this.BuyOrSell)
                {
                    if (st is StockPurchased)
                    {
                        avgCost = (avgCost * owned + st.Price * st.NumberOfShares)
                            / (owned + st.NumberOfShares);
                        
                        owned += st.NumberOfShares;
                    }
                    else
                    {
                        owned -= st.NumberOfShares;
                    }
                }

                return avgCost;
            }
        }
        public string Description => $"{this.Owned} shares owned at avg cost {Math.Round(this.AverageCost, 2)}";

        internal void Apply(StockPurchased purchased)
        {
            this.BuyOrSell.Add(purchased);

            this.Transactions.Add(Transaction.DebitTx(
                this.Id,
                purchased.Id,
                this.Ticker,
                $"Purchased {purchased.NumberOfShares} shares @ ${purchased.Price}/share",
                purchased.Price * purchased.NumberOfShares,
                purchased.When,
                false
            ));
        }

        internal void Apply(StockDeleted deleted)
        {
            this.Transactions.Clear();
            this.BuyOrSell.Clear();
        }

        internal void Apply(StockSold sold)
        {
            this.BuyOrSell.Add(sold);
            
            this.Transactions.Add(Transaction.CreditTx(
                this.Id,
                sold.Id,
                this.Ticker,
                $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                sold.Price * sold.NumberOfShares,
                sold.When,
                false
            ));

            this.Transactions.Add(Transaction.PLTx(
                this.Id,
                this.Ticker,
                $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                this.AverageCost * sold.NumberOfShares,
                sold.Price * sold.NumberOfShares,
                sold.When,
                false
            ));

            if (this.Owned == 0)
            {
                this.BuyOrSell.Clear();
            }
        }
    }
}