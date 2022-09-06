using System;
using core.Shared;

namespace core.Portfolio.Output
{
    public class PortfolioResponse : IViewModel
    {
        public int OpenStockCount { get; set; }
        public int OpenCryptoCount { get; set; }
        public int OpenOptionCount { get; set; }
        public int TriggeredAlertCount { get; set; }
        public int AlertCount { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}