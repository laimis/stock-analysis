using System.Collections.Generic;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using iexclient;
using testutils;

namespace iexclienttests
{
    public class IEXClientFixture
    {
        public string[] Options;
        public IEnumerable<OptionDetail> OptionDetails;
        public Price Price;
        public IEXClient Client;
        public List<StockQueryResult> MostActive;
        public List<SearchResult> SearchResults;
        public StockAdvancedStats AdvancedStats;

        public IEXClientFixture()
        {
            Client = new IEXClient(CredsHelper.GetIEXToken(), logger: null, useCache: false);

            var t = Client.GetOptions("AMD");

            t.Wait();
            
            Options = t.Result;

            var dt = Client.GetOptionDetails("AMD", "20210806");

            dt.Wait();

            OptionDetails = dt.Result;

            var price = Client.GetPrice("AMD");

            price.Wait();

            Price = price.Result.Success;

            var search = Client.Search("stitch", 5);

            search.Wait();

            SearchResults = search.Result.Success;

            var advancedStats = Client.GetAdvancedStats("GOOGL");

            advancedStats.Wait();

            AdvancedStats = advancedStats.Result.Success;
        }
    }
}
