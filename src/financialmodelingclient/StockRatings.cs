using System.Collections.Generic;

namespace financialmodelingclient
{
	public class StockRatings
	{
		public StockRating Rating { get; set; }
		public Dictionary<string, StockRating> RatingDetails { get; set; }
	}
}