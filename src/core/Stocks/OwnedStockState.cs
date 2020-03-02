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
		public int Owned { get; internal set; }
		public double Spent { get; internal set; }
		public DateTimeOffset LastPurchase { get; internal set; }
        public DateTimeOffset? LastSale { get; internal set; }

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

            Owned += purchased.NumberOfShares;
            Spent += purchased.NumberOfShares * purchased.Price;
            
            LastPurchase = purchased.When;

            this.Buys.Add(purchased);

            this.Transactions.Add(Transaction.DebitTx(
                this.Ticker,
                $"Purchased {purchased.NumberOfShares} shares @ ${purchased.Price}/share",
                purchased.Price * purchased.NumberOfShares,
                purchased.When,
                false
            ));
        }

        internal void Apply(StockDeleted deleted)
        {
            this.Owned = 0;
            this.Transactions.Clear();
            this.AverageCost = 0;
        }

        internal void Apply(StockSold sold)
        {
            this.Owned -= sold.NumberOfShares;
            this.LastSale = sold.When;

            if (this.Owned == 0)
            {
                this.AverageCost = 0;
            }

            this.Spent -= sold.NumberOfShares * sold.Price;

            this.Sells.Add(sold);
            
            this.Transactions.Add(Transaction.CreditTx(
                this.Ticker,
                $"Sold {sold.NumberOfShares} shares @ ${sold.Price}/share",
                sold.Price * sold.NumberOfShares,
                sold.When,
                false
            ));
        }
    }
}