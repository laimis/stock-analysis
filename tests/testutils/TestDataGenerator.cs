using System;
using System.IO;
using System.Linq;
using core.fs.Adapters.Stocks;
using core.fs.Accounts;
using core.Shared;

namespace testutils
{
    public class TestDataGenerator
    {
        public const string TestDataPath = "./testdata";
        
        public static PriceBars PriceBars(Ticker ticker)
        {
            var content = File.ReadAllText($"{TestDataPath}/prices/{ticker.Value}.csv");

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
        public static Ticker AMD = new("amd");
        public static Ticker NET = new("net");
        public static Ticker ENPH = new("enph");
        public static Ticker TEUM = new("teum");
        public static TradeGrade A = new("A");
        public static TradeGrade B = new("B");

        public static UserId RandomUserId() => UserId.NewUserId(Guid.NewGuid());
        
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public static Ticker GenerateRandomTicker(Random random)
        {
            var length = random.Next(3, 5) + 1; // inserting '0' at the end to make sure we don't generate real tickers 
            var ticker = new char[length];
            for (var i = 0; i < length; i++)
            {
                ticker[i] = Chars[random.Next(Chars.Length)];
            }
            ticker[length - 1] = '0';

            return new Ticker(new string(ticker));
        }
    }
}
