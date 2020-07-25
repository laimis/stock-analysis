using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;

namespace core.Stocks
{
    public class OwnedStockState
	{
        public OwnedStockState()
        {
            this.Transactions = new List<Transaction>();
            this.Buys = new List<StockPurchased>();
            this.Sells = new List<StockSold>();
        }

		public string Ticker { get; internal set; }
		public Guid UserId { get; internal set; }
		public int Owned => this.Buys.Sum(b => b.NumberOfShares) - this.Sells.Sum(s => s.NumberOfShares);
		public double Cost => this.Owned * this.AverageCost;
		
        public List<Transaction> Transactions { get; private set; }
        
        public Guid Id { get; set; }
        public double AverageCost { get; private set; }
        public string Description => $"{this.Owned} shares owned at avg cost {Math.Round(this.AverageCost, 2)}";
        internal List<StockPurchased> Buys { get; }
        internal List<StockSold> Sells { get; }

        internal void Apply(StockPurchased purchased)
        {
            this.AverageCost = 
                (this.AverageCost * this.Owned + purchased.Price * purchased.NumberOfShares) 
                / (this.Owned + purchased.NumberOfShares);

            this.Buys.Add(purchased);

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
            this.Buys.Clear();
            this.Sells.Clear();
            this.AverageCost = 0;
        }

        internal void Apply(StockSold sold)
        {
            this.Sells.Add(sold);
            
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
                sold.Id,
                this.Ticker,
                $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                this.AverageCost * sold.NumberOfShares,
                sold.Price * sold.NumberOfShares,
                sold.When,
                false
            ));

            if (this.Owned == 0)
            {
                this.AverageCost = 0;
            }
        }
    }
}