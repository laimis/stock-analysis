using System;

namespace core.Stocks
{
	public class OwnedStockState
	{
		public string Ticker { get; internal set; }
		public string UserId { get; internal set; }
		public int Owned { get; internal set; }
		public double Spent { get; internal set; }
		public double Earned { get; internal set; }
        public DateTime Purchased { get; internal set; }
        public DateTime? Sold { get; internal set; }
        public double Profit => this.Sold != null ? this.Earned - this.Spent : 0;
    }
}