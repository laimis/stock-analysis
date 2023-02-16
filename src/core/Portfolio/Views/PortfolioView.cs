using System;
using core.Shared;

namespace core.Portfolio.Views
{
    public class PortfolioView : IViewModel
    {
        public int OpenStockCount { get; set; }
        public int OpenCryptoCount { get; set; }
        public int OpenOptionCount { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}