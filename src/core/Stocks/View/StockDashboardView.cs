using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Stocks.View
{
    public class StockDashboardView : IViewModel
    {
        public StockDashboardView() {}
        public StockDashboardView(List<OwnedStockView> owned, List<PositionInstance> positions)
        {
            Owned = owned;
            Positions = positions;
            Calculated = DateTimeOffset.UtcNow;
        }

        public List<OwnedStockView> Owned { get; set; }
        public List<PositionInstance> Positions { get; }
        public DateTimeOffset Calculated { get; set; }
    }
}