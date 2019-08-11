using System.Collections.Generic;

namespace web.Services
{
	internal class StockRatings
	{
		public StockRating Rating { get; set; }
		public Dictionary<string, StockRating> RatingDetails { get; set; }
	}
}