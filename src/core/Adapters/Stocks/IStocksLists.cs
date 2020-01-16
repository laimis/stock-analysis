using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Adapters.Stocks
{
    public interface IStocksLists
    {
         Task<List<MostActiveEntry>> GetMostActive();
    }
}