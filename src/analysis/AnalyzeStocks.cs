namespace analysis
{
	public class AnalyzeStocks
	{
		public AnalyzeStocks(float priceLevel, float bookValuePremium, float? peRatio = null)
		{
			this.PriceLevel = priceLevel;
			this.BookValuePremium = bookValuePremium;
			this.PERatio = peRatio;
		}

		public float PriceLevel { get; }
		public float BookValuePremium { get; }
		public float? PERatio { get; }
	}
}