namespace core.Portfolio
{
	public class OwnedStockState
	{
		public string Ticker { get; internal set; }
		public string UserId { get; internal set; }
		public int Owned { get; internal set; }
		public double Spent { get; internal set; }
	}
}