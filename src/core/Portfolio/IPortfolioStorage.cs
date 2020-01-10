using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Portfolio
{
    public interface IPortfolioStorage
    {
        Task<OwnedStock> GetStock(string ticker, string userId);
        Task<IEnumerable<OwnedStock>> GetStocks(string userId);
        Task Save(OwnedStock stock);

        Task<IEnumerable<SoldOption>> GetSoldOptions(string user);
        Task<SoldOption> GetSoldOption(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice, string userId);
        Task Save(SoldOption option);

        Task DoHealthCheck();
    }
}