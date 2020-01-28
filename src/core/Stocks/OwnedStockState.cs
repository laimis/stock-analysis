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
            this.Buys = new List<StockPurchased>();
            this.Sells = new List<StockSold>();
        }

		public string Ticker { get; internal set; }
		public string UserId { get; internal set; }
		public int Owned { get; internal set; }
		public double Spent { get; internal set; }
		public double Earned { get; internal set; }
        public DateTimeOffset Purchased { get; internal set; }
        public DateTimeOffset? Sold { get; internal set; }
        public double Profit => this.Sold != null ? this.Earned - this.Spent : 0;

        public List<Transaction> Transactions { get; private set; }
        public double Cost => Math.Round(Math.Abs(this.Spent - this.Earned), 2);

        public Guid Id { get; set; }
        internal List<StockPurchased> Buys { get; }
        internal List<StockSold> Sells { get; }

        internal void Apply(StockPurchased purchased)
        {
            Owned += purchased.Amount;
            Spent += purchased.Amount * purchased.Price;
            Purchased = purchased.When;

            this.Buys.Add(purchased);

            this.Transactions.Add(new Transaction(
                this.Ticker,
                $"Purchased {purchased.Amount} shares @ ${purchased.Price}/share",
                purchased.Price * purchased.Amount,
                0,
                purchased.When
            ));
        }

        internal void Apply(StockSold sold)
        {
            this.Owned -= sold.Amount;
            this.Earned += sold.Amount * sold.Price;
            this.Sold = sold.When;

            this.Sells.Add(sold);
            
            this.Transactions.Add(new Transaction(
                this.Ticker,
                $"Sold {sold.Amount} shares @ ${sold.Price}/share",
                sold.Price * sold.Amount,
                this.Profit,
                sold.When
            ));
        }
    }
}