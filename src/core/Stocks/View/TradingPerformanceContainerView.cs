using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.View
{
    public class TradingPerformanceContainerView
    {
        public TradingPerformanceContainerView() {}
        public TradingPerformanceContainerView(List<PositionInstance> closedTransactions, int recentCount)
        {
            Recent = new TradingPerformanceView(closedTransactions.OrderByDescending(p => p.Closed).Take(recentCount).ToList());
            Overall = new TradingPerformanceView(closedTransactions);
        }

        public TradingPerformanceView Recent { get; set; }
        public TradingPerformanceView Overall { get; set; }
    }
}