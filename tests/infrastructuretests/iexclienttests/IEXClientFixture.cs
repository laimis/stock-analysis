using System.Collections.Generic;
using core.Options;
using iexclient;

namespace iexclienttests
{
    public class IEXClientFixture
    {
        public string[] Options;
        public IEnumerable<OptionDetail> OptionDetails;

        public IEXClientFixture()
        {
            var client = new IEXClient("add-your-key");

            var t = client.GetOptions("TEUM");

            t.Wait();
            
            Options = t.Result;

            var dt = client.GetOptionDetails("TEUM", "201909");

            dt.Wait();

            OptionDetails = dt.Result;
        }
    }
}
