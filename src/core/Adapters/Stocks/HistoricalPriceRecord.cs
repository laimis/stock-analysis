using System;

namespace core.Adapters.Stocks
{
	public class HistoricalPriceRecord
	{
		public DateTime Date { get; set; }
		public float Close { get; set; }
		public long Volume { get; set; }
		public float ChangePercent { get; set; }
	}
}