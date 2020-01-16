using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Adapters.Stocks;

namespace coretests.Fakes
{
    internal class FakeStocksLists : IStocksLists
    {
        private MostActiveEntry _registered;

        public FakeStocksLists()
        {
        }

        public Task<List<MostActiveEntry>> GetMostActive()
        {
            return Task.FromResult(new List<MostActiveEntry>{_registered});
        }

        internal void Register(MostActiveEntry mostActiveEntry)
        {
            _registered = mostActiveEntry;
        }
    }
}