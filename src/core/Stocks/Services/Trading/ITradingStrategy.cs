using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public interface ITradingStrategy
    {
        TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success);
    }

    public record struct TradingStrategyResults
    {
        public List<TradingStrategyResult> Results { get; init; }
    }

    public record struct TradingStrategyResult(
        decimal maxDrawdownPct,
        decimal maxGainPct,
        PositionInstance position,
        string strategyName
    );

    public class TradingStrategy : ITradingStrategy
    {
        public TradingStrategy(string name, Func<PositionInstance, PriceBar[], TradingStrategyResult> runFunc)
        {
            Name = name;
            RunFunc = runFunc;
        }

        public string Name { get; }
        public Func<PositionInstance, PriceBar[], TradingStrategyResult> RunFunc { get; }

        public TradingStrategyResult Run(PositionInstance positionInstance, PriceBar[] success)
        {
            return RunFunc(positionInstance, success);
        }
    }

    public class TradingStrategyFactory
    {
        public static ITradingStrategy Create(string name)
        {
            // TODO: use name to look up strategy
            return new TradingStrategy(
                TradingStrategyRRLevels.StrategyNameOneThirdRR,
                TradingStrategyRRLevels.RunOneThirdRR
            );
        }
    }
}