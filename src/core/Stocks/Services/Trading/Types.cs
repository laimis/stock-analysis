using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public static class TradingStrategyConstants
    {
        // TODO: this needs to come from the environment or user settings
        public const decimal AVG_PERCENT_GAIN = 0.07m;
        public const decimal DEFAULT_STOP_PRICE_MULTIPLIER = 0.95m;
        public const int MAX_NUMBER_OF_DAYS_TO_SIMULATE = 365;
    }

    public interface ITradingStrategy
    {
        TradingStrategyResult Run(PositionInstance position, IEnumerable<PriceBar> bars);
    }

    public class TradingStrategyResults
    {
        public List<TradingStrategyResult> Results { get; } = new List<TradingStrategyResult>();

        internal void Add(TradingStrategyResult result) => Results.Add(result);

        internal void Insert(int index, TradingStrategyResult result) =>
            Results.Insert(index, result);
    }

    public record struct TradingStrategyPerformance(
        string strategyName,
        TradingPerformanceView performance,
        PositionInstance[] positions
    );

    public record struct TradingStrategyResult(
        decimal maxDrawdownPct,
        decimal maxGainPct,
        PositionInstance position,
        string strategyName
    );

    internal record struct SimulationContext(
        PositionInstance Position,
        decimal MaxGain,
        decimal MaxDrawdown
    );
}