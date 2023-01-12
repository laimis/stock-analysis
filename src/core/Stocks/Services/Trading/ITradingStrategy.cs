using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public static class TradingStrategyConstants
    {
        // TODO: this needs to come from the environment or user settings
        public const decimal AVG_PERCENT_GAIN = 0.07m;
    }
    
    public interface ITradingStrategy
    {
        TradingStrategyResult Run(
            PositionInstance position,
            PriceBar[] bars,
            bool closeIfOpenAtTheEnd
        );
    }

    public class TradingStrategyResults
    {
        public List<TradingStrategyResult> Results { get; } = new List<TradingStrategyResult>();
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

    public class TradingStrategy : ITradingStrategy
    {
        public TradingStrategy(Func<PositionInstance, PriceBar[], bool, TradingStrategyResult> runFunc)
        {
            RunFunc = runFunc;
        }

        public Func<PositionInstance, PriceBar[], bool, TradingStrategyResult> RunFunc { get; }

        public TradingStrategyResult Run(
            PositionInstance position,
            PriceBar[] bars,
            bool closeIfOpenAtTheEnd)
        {
            return RunFunc(position, bars, closeIfOpenAtTheEnd);
        }
    }
}