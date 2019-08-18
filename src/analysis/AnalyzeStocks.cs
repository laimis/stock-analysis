namespace analysis
{
	public class AnalyzeStocks
	{
		public AnalyzeStocks(float priceLevel, float bookValuePremium)
		{
			this.PriceLevel = priceLevel;
			this.BookValuePremium = bookValuePremium;
		}

		public float PriceLevel { get; }
		public float BookValuePremium { get; }
	}
}