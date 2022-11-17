using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Shared.Adapters.Cryptos
{
    public static class ListingsExtension
    {
        public static bool TryGet(this Listings listings, string token, out Price? price)
        {
            price = null;
            
            var data = listings.data.SingleOrDefault(d => d.symbol == token);
            if (data != null)
            {
                price = new Price(data.quote.usd.price);
                return true;
            }

            return false;
        }
    }
    public class Listings
    {
        public Status status { get; set; }

        public List<Datum> data { get; set; }
    }

    public partial class Datum
    {
        public long id { get; set; }

        public string name { get; set; }

        public string symbol { get; set; }

        public string slug { get; set; }

        public long num_market_pairs { get; set; }

        public DateTimeOffset date_added { get; set; }

        public List<string> tags { get; set; }

        public long? max_supply { get; set; }

        public double circulating_supply { get; set; }

        public double total_supply { get; set; }

        public Platform platform { get; set; }

        public long cmc_rank { get; set; }

        public DateTimeOffset last_updated { get; set; }

        public Quote quote { get; set; }
    }

    public partial class Platform
    {
        public long id { get; set; }

        public string name { get; set; }

        public string symbol { get; set; }

        public string slug { get; set; }

        public string token_address { get; set; }
    }

    public partial class Quote
    {
        public Usd usd { get; set; }
    }

    public partial class Usd
    {
        public decimal price { get; set; }

        public double volume_24h { get; set; }

        public double percent_change_1h { get; set; }

        public double percent_change_24h { get; set; }

        public double percent_change_7d { get; set; }

        public double percent_change_30d { get; set; }

        public double percent_change_60d { get; set; }

        public double percent_change_90d { get; set; }

        public double market_cap { get; set; }

        public double market_cap_dominance { get; set; }

        public double fully_diluted_market_cap { get; set; }

        public DateTimeOffset last_updated { get; set; }
    }

    public partial class Status
    {
        public DateTimeOffset timestamp { get; set; }

        public long error_code { get; set; }

        public object error_message { get; set; }

        public long elapsed { get; set; }

        public long credit_count { get; set; }

        public object notice { get; set; }

        public long total_count { get; set; }
    }
}
