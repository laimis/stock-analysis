using System.Collections.Generic;
using System.Linq;

namespace core.Stocks
{
	public class StockListResponse
	{
		public List<StockInformation> SymbolsList { get; set; }
		public IEnumerable<StockInformation> FilteredList => this.SymbolsList.Where(s => !s.Name.Contains(" ETF"));
	}
}