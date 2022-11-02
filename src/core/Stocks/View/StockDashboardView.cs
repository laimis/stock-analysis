using System;
using System.Collections.Generic;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.View
{
    public class StockDashboardView : IViewModel
    {
        public StockDashboardView() {}
        public StockDashboardView(List<PositionInstance> positions)
        {
            Positions = positions;
            Calculated = DateTimeOffset.UtcNow;
        }

        public List<PositionInstance> Positions { get; set; }
        public DateTimeOffset Calculated { get; set; }
        public List<StockViolationView> Violations { get; private set; }
        public Order[] Orders { get; private set; }

        internal void SetOrders(Order[] brokerageOrders)
        {
            Orders = brokerageOrders;
        }

        internal void SetViolations(List<StockViolationView> violations)
        {
            Violations = violations;
        }
    }

    public record struct StockViolationView(
        string message,
        decimal numberOfShares,
        decimal pricePerShare,
        string ticker);
}