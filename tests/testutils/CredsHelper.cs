using System;
using System.IO;

namespace testutils
{
    public class CredsHelper
    {
        public static string GetIEXToken()
        {
            var path = @"..\..\..\..\..\..\iex_secret";

            return File.ReadAllText(path);
        }

        public static string GetTDAmeritradeConfig()
        {
            var path = @"..\..\..\..\..\..\tdameritrade_secret";

            return File.ReadAllText(path);
        }

        public static string GetCoinMarketCapToken()
        {
            var path = @"..\..\..\..\..\..\coinmarketcap_secret";

            return File.ReadAllText(path);
        }

        public static string GetDbCreds()
        {
            var val = Environment.GetEnvironmentVariable("DB_CNN");
            if (val != null)
            {
                return val;
            }

            var path = @"..\..\..\..\..\postgres_secret";

            return File.ReadAllText(path);
        }
    }
}