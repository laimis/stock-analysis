using System;
using core.Shared;

namespace core.Portfolio.Views
{
    public class PortfolioView : IViewModel
    {
        public static string Version = "1";
        
        public int OpenStockCount { get; set; }
        public int OpenCryptoCount { get; set; }
        public int OpenOptionCount { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}