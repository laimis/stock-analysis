using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public interface ITradingStrategy
    {
        TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success, bool closeIfOpenAtTheEnd);
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

        public TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success, bool closeIfOpenAtTheEnd)
        {
            return RunFunc(positionInstance, success, closeIfOpenAtTheEnd);
        }
    }

    public class TradingStrategyFactory
    {
        public static IEnumerable<ITradingStrategy> GetStrategies()
        {
            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneThirdRR
            );

            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneThirdRRDelayedStop
            );

            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneFourthRR
            );

            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneThirdPercentBased
            );

            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneFourthPercentBased
            );


        }
    }
}