using System;
using System.Linq;
using core.Adapters.Options;

namespace core.Options
{
    public class OptionDetailsViewModel
    {
        public OptionDetailsViewModel(decimal? price, OptionChain chain)
        {
            StockPrice = price;
            Options = chain.Options;
            Expirations = chain.Options.Select(o => o.ExpirationDate).Distinct().ToArray();
            Volatility = chain.Volatility;
            NumberOfContracts = chain.NumberOfContracts;
        }
        public decimal? StockPrice { get; }
        public OptionDetail[] Options { get; }
        public string[] Expirations { get; }
        public decimal Volatility { get; }
        public decimal NumberOfContracts { get; }
    }
}