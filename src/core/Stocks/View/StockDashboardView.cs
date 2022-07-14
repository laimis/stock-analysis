using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    public class StockDashboardView : IViewModel
    {
        public List<OwnedStockView> Owned { get; set; }
        public StockOwnershipPerformanceContainerView Performance { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}