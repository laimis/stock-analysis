using System;
using System.Collections.Generic;
using core;
using core.Adapters.Options;
using core.Adapters.Stocks;
using iexclient;

namespace iexclienttests
{
    public class IEXClientFixture
    {
        public string[] Options;
        public IEnumerable<OptionDetail> OptionDetails;
        public TickerPrice Price;
        public IEXClient Client;
        public List<StockQueryResult> MostActive;
        public List<SearchResult> SearchResults;

        public IEXClientFixture()
        {
            Client = new IEXClient("pk_71ba0d8d98ed4d2caac8089588d62973");

            var t = Client.GetOptions("TEUM");

            t.Wait();
            
            Options = t.Result;

            var dt = Client.GetOptionDetails("TEUM", "201909");

            dt.Wait();

            OptionDetails = dt.Result;

            var price = Client.GetPrice("TEUM");

            price.Wait();

            Price = price.Result;

            var search = Client.Search("stitch");

            search.Wait();

            SearchResults = search.Result;
        }
    }
}
