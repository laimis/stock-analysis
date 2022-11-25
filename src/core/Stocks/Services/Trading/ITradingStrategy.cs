using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public interface ITradingStrategy
    {
        TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success);
    }

    public class TradingStrategyResults
    {
        public List<TradingStrategyResult> Results { get; } = new List<TradingStrategyResult>();
    }

    public record struct TradingStrategyResult(
        decimal maxDrawdownPct,
        decimal maxGainPct,
        PositionInstance position,
        string strategyName
    );

    public class TradingStrategy : ITradingStrategy
    {
        public TradingStrategy(string name, Func<string, PositionInstance, PriceBar[], TradingStrategyResult> runFunc)
        {
            Name = name;
            RunFunc = runFunc;
        }

        public string Name { get; }
        public Func<string, PositionInstance, PriceBar[], TradingStrategyResult> RunFunc { get; }

        public TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success)
        {
            return RunFunc(Name, positionInstance, success);
        }
    }

    public class TradingStrategyFactory
    {
        public static IEnumerable<ITradingStrategy> GetStrategies()
        {
            yield return new TradingStrategy(
                TradingStrategyRRLevels.StrategyNameOneThirdRR,
                TradingStrategyRRLevels.RunOneThirdRR
            );

            yield return new TradingStrategy(
                TradingStrategyRRLevels.StrategyNameOneFourthRR,
                TradingStrategyRRLevels.RunOneFourthRR
            );
        }
    }
}