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
        public List<PositionInstance> Positions { get; set; }
        public DateTimeOffset Calculated { get; set; }
        public List<StockViolationView> Violations { get; private set; }
        public BrokerageOrderView[] Orders { get; private set; }

        internal void SetOrders(BrokerageOrderView[] brokerageOrders)
        {
            Orders = brokerageOrders;
        }

        internal void SetViolations(List<StockViolationView> violations)
        {
            Violations = violations;
        }
    }

    public class StockViolationView
    {
        public string Ticker { get; set; }
        public string Message { get; set; }
    }
}