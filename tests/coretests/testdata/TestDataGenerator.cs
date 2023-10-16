using System;
using System.IO;
using System.Linq;
using core.fs.Shared.Adapters.Stocks;

namespace coretests.testdata
{
    public class TestDataGenerator
    {
        public static PriceBar[] PriceBars(string ticker = "NET")
        {
            var content = File.ReadAllText($"testdata/pricefeed_{ticker}.txt");

            return content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(PriceBar.Parse)
                .ToArray();
        }

        public static PriceBar[] IncreasingPriceBars(int numOfBars = 10)
        {
            return 
                Enumerable
                    .Range(0, numOfBars)
                    .Select(
                    x =>
                        new PriceBar(
                            date: DateTime.Now.AddDays(-numOfBars).AddDays(x),
                            open: x,
                            high: x,
                            low: x == 0 ? x : x - 1,
                            close: x,
                            volume: x
                        )
                    )
                .ToArray();
        }
        
        public static string RandomEmail() => $"{Guid.NewGuid().ToString()}@gmail.com";
    }
}