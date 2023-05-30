using System;
using System.IO;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace coretests.TestData
{
    public class TestDataGenerator
    {
        public static PriceBar[] PriceBars(string ticker = "NET")
        {
            var content = File.ReadAllText($"testdata/pricefeed_{ticker}.txt");

            return content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => PriceBar.Parse(x))
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
    }
}