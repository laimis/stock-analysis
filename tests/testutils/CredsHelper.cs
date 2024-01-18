using System;
using System.IO;

namespace testutils
{
    public class CredsHelper
    {
        public static string GetCoinMarketCapToken()
        {
            var path = @"..\..\..\..\..\..\coinmarketcap_secret";

            return File.ReadAllText(path);
        }

        public static string GetDbCreds()
        {
            var path = @"..\..\..\..\..\postgres_secret";

            return File.ReadAllText(path);
        }
    }
}