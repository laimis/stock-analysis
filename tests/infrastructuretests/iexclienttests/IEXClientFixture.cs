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
        public List<MostActiveEntry> MostActive;

        public IEXClientFixture()
        {
            Client = new IEXClient("<enter key>");

            var t = Client.GetOptions("TEUM");

            t.Wait();
            
            Options = t.Result;

            var dt = Client.GetOptionDetails("TEUM", "201909");

            dt.Wait();

            OptionDetails = dt.Result;

            var price = Client.GetPrice("TEUM");

            price.Wait();

            Price = price.Result;

            var active = Client.GetMostActive();

            active.Wait();

            MostActive = active.Result;
        }
    }
}
