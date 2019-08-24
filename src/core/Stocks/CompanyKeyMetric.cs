using System;
using Newtonsoft.Json;

namespace core.Stocks
{
	public class CompanyKeyMetric
	{
		public DateTime Date { get; set; }
		
		[JsonProperty("Revenue per Share")]
		public float? RevenuePerShare { get; set; }

		[JsonProperty("Book Value per Share")]
		public float? BookValuePerShare { get; set; }

		[JsonProperty("PE ratio")]
		public float? PERatio { get; set; }
	}
}