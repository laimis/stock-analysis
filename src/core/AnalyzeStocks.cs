namespace core
{
	public class AnalyzeStocks
	{
		public AnalyzeStocks(float minPrice, float maxPrice)
		{
			this.MinPrice = minPrice;
			this.MaxPrice = maxPrice;
		}

		public float MinPrice { get; }
		public float MaxPrice { get; }
	}
}