using System;
using System.IO;
using System.Linq;
using core.fs.Shared.Adapters.Stocks;
using core.fs.Shared.Domain.Accounts;
using core.Shared;

namespace coretests.testdata
{
    public class TestDataGenerator
    {
        public static PriceBars PriceBars(Ticker ticker)
        {
            var content = File.ReadAllText($"testdata/pricefeed_{ticker.Value}.txt");

            var array = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new PriceBar(s))
                .ToArray();

            return new PriceBars(array);
        }

        public static PriceBars IncreasingPriceBars(int numOfBars = 10)
        {
            var array =
                Enumerable
                    .Range(0, numOfBars)
                    .Select(
                    x =>
                        new PriceBar(
                            date: DateTime.Now.AddDays(-numOfBars).AddDays(x),
                            x,
                            high: x,
                            low: x == 0 ? x : x - 1,
                            close: x,
                            volume: x
                        )
                    )
                .ToArray();
            
            return new PriceBars(array);
        }
        
        public static string RandomEmail() => $"{Guid.NewGuid().ToString()}@gmail.com";
        public static Ticker TSLA = new("tsla");
        public static Ticker NET = new("net");
        public static Ticker ENPH = new("enph");
        public static Ticker TEUM = new("teum");
        public static TradeGrade A = new("A");
        public static TradeGrade B = new("B");

        public static UserId RandomUserId() => UserId.NewUserId(Guid.NewGuid());
    }
}