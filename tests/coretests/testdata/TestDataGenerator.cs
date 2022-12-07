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
    }
}