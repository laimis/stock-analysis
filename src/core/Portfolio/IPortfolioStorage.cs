using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Portfolio
{
	public interface IPortfolioStorage
	{
		Task<OwnedStock> GetStock(string ticker, string userId);
		Task<IEnumerable<OwnedStock>> GetStocks(string userId);
	}
}