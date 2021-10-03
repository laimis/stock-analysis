using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace core.Shared.Adapters.Cryptos
{
    public static class ListingsExtension
    {
        public static bool TryGet(this Listings listings, string token, out Price? price)
        {
            price = null;
            
            var data = listings.Data.SingleOrDefault(d => d.Symbol == token);
            if (data != null)
            {
                price = new Price(data.Quote.Usd.Price);
                return true;
            }

            return false;
        }
    }
    public class Listings
    {
        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("data")]
        public List<Datum> Data { get; set; }
    }

    public partial class Datum
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("num_market_pairs")]
        public long NumMarketPairs { get; set; }

        [JsonPropertyName("date_added")]
        public DateTimeOffset DateAdded { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; }

        [JsonPropertyName("max_supply")]
        public long? MaxSupply { get; set; }

        [JsonPropertyName("circulating_supply")]
        public double CirculatingSupply { get; set; }

        [JsonPropertyName("total_supply")]
        public double TotalSupply { get; set; }

        [JsonPropertyName("platform")]
        public Platform Platform { get; set; }

        [JsonPropertyName("cmc_rank")]
        public long CmcRank { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonPropertyName("quote")]
        public Quote Quote { get; set; }
    }

    public partial class Platform
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("token_address")]
        public string TokenAddress { get; set; }
    }

    public partial class Quote
    {
        [JsonPropertyName("USD")]
        public Usd Usd { get; set; }
    }

    public partial class Usd
    {
        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("volume_24h")]
        public double Volume24H { get; set; }

        [JsonPropertyName("percent_change_1h")]
        public double PercentChange1H { get; set; }

        [JsonPropertyName("percent_change_24h")]
        public double PercentChange24H { get; set; }

        [JsonPropertyName("percent_change_7d")]
        public double PercentChange7D { get; set; }

        [JsonPropertyName("percent_change_30d")]
        public double PercentChange30D { get; set; }

        [JsonPropertyName("percent_change_60d")]
        public double PercentChange60D { get; set; }

        [JsonPropertyName("percent_change_90d")]
        public double PercentChange90D { get; set; }

        [JsonPropertyName("market_cap")]
        public double MarketCap { get; set; }

        [JsonPropertyName("market_cap_dominance")]
        public double MarketCapDominance { get; set; }

        [JsonPropertyName("fully_diluted_market_cap")]
        public double FullyDilutedMarketCap { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }
    }

    public partial class Status
    {
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("error_code")]
        public long ErrorCode { get; set; }

        [JsonPropertyName("error_message")]
        public object ErrorMessage { get; set; }

        [JsonPropertyName("elapsed")]
        public long Elapsed { get; set; }

        [JsonPropertyName("credit_count")]
        public long CreditCount { get; set; }

        [JsonPropertyName("notice")]
        public object Notice { get; set; }

        [JsonPropertyName("total_count")]
        public long TotalCount { get; set; }
    }
}
