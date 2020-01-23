using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks
{
	public class OwnedStockState
	{
        public OwnedStockState()
        {
            this.Transactions = new List<Transaction>();
        }

		public string Ticker { get; internal set; }
		public string UserId { get; internal set; }
		public int Owned { get; internal set; }
		public double Spent { get; internal set; }
		public double Earned { get; internal set; }
        public DateTime Purchased { get; internal set; }
        public DateTime? Sold { get; internal set; }
        public double Profit => this.Sold != null ? this.Earned - this.Spent : 0;

        public List<Transaction> Transactions { get; private set; }

        internal void Apply(StockPurchased purchased)
        {
            Owned += purchased.Amount;
            Spent += purchased.Amount * purchased.Price;
            Purchased = purchased.When;

            this.Transactions.Add(new Transaction(
                this.Ticker,
                "Purchased shares",
                -1 * purchased.Price * purchased.Amount,
                purchased.When
            ));
        }

        internal void Apply(StockSold sold)
        {
            this.Owned -= sold.Amount;
            this.Earned += sold.Amount * sold.Price;
            this.Sold = sold.When;

            this.Transactions.Add(new Transaction(
                this.Ticker,
                "Sold shares",
                sold.Price * sold.Amount,
                sold.When
            ));
        }
    }
}