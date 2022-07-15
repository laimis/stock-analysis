using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    public class StockDashboardView : IViewModel
    {
        public StockDashboardView() {}
        public StockDashboardView(List<OwnedStockView> owned)
        {
            Owned = owned;
            Calculated = DateTimeOffset.UtcNow;
        }

        public List<OwnedStockView> Owned { get; set; }
        public DateTimeOffset Calculated { get; set; }
    }
}