using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Options;
using core.Stocks;

namespace coretests.Fakes
{
    public class FakePortfolioStorage : IPortfolioStorage
    {
        private SoldOption _soldOption;
        private List<SoldOption> _savedSoldOptions = new List<SoldOption>();

        public IEnumerable<SoldOption> SavedOptions => _savedSoldOptions.AsReadOnly();

        public Task<SoldOption> GetSoldOption(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice, string userId)
        {
            return Task.FromResult<SoldOption>(_soldOption);
        }

        public Task<IEnumerable<SoldOption>> GetSoldOptions(string user)
        {
            return Task.FromResult(
                new List<SoldOption>{_soldOption}.Select(o => o)
            );
        }

        public Task<OwnedStock> GetStock(string ticker, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            throw new NotImplementedException();
        }

        internal void Register(SoldOption opt)
        {
            _soldOption = opt;
        }

        public Task Save(OwnedStock stock)
        {
            throw new NotImplementedException();
        }

        public Task Save(SoldOption option)
        {
            _savedSoldOptions.Add(option);

            return Task.CompletedTask;
        }
    }
}