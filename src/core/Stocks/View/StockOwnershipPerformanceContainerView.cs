using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.View
{
    public class StockOwnershipPerformanceContainerView
    {
        public StockOwnershipPerformanceContainerView() {}
        public StockOwnershipPerformanceContainerView(List<PositionInstance> closedTransactions)
        {
            Recent = new StockOwnershipPerformanceView(closedTransactions.OrderByDescending(p => p.Closed).Take(20).ToList());
            Overall = new StockOwnershipPerformanceView(closedTransactions.Take(20).ToList());
        }

        public StockOwnershipPerformanceView Recent { get; set; }
        public StockOwnershipPerformanceView Overall { get; set; }
    }
}