using System.Collections.Generic;

namespace core.Stocks
{
	public class StockRatings
	{
		public StockRating Rating { get; set; }
		public Dictionary<string, StockRating> RatingDetails { get; set; }
	}
}