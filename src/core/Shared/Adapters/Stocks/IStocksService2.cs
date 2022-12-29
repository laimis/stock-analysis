using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared;

namespace core.Adapters.Stocks
{
    public interface IStocksService2
    {
        Task<ServiceResponse<Dictionary<string, BatchStockPrice>>> GetPrices(IEnumerable<string> tickers);
    }
}