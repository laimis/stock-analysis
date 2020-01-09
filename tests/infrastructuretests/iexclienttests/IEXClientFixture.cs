using System.Collections.Generic;
using core.Options;
using iexclient;

namespace iexclienttests
{
    public class IEXClientFixture
    {
        public string[] Options;
        public IEnumerable<OptionDetail> OptionDetails;
        public double Price;
        public IEXClient Client;

        public IEXClientFixture()
        {
            Client = new IEXClient("your-key");

            var t = Client.GetOptions("TEUM");

            t.Wait();
            
            Options = t.Result;

            var dt = Client.GetOptionDetails("TEUM", "201909");

            dt.Wait();

            OptionDetails = dt.Result;

            var price = Client.GetPrice("TEUM");

            price.Wait();

            Price = price.Result;
        }
    }
}
