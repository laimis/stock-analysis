using System.Collections.Generic;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using core.Stocks.Services.Trading;
using core.Stocks.View;

namespace core.Portfolio.Views
{
    public record TradingEntriesView(
            PositionInstance[] current,
            PositionInstance[] past,
            TradingPerformanceContainerView performance,
            List<StockViolationView> violations,
            TradingStrategyPerformance[] strategyPerformance,
            decimal? cashBalance,
            Order[] brokerageOrders);
}