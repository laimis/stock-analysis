using System;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public interface ITradingStrategy
    {
        PositionInstance Run(PositionInstance positionInstance, PriceBar[] success);
    }

    public class TradingStrategy : ITradingStrategy
    {
        public TradingStrategy(string name, Func<PositionInstance, PriceBar[], PositionInstance> runFunc)
        {
            Name = name;
            RunFunc = runFunc;
        }

        public string Name { get; }
        public Func<PositionInstance, PriceBar[], PositionInstance> RunFunc { get; }

        public PositionInstance Run(PositionInstance positionInstance, PriceBar[] success)
        {
            return RunFunc(positionInstance, success);
        }
    }

    public class TradingStrategyFactory
    {
        public static ITradingStrategy Create(string name)
        {
            // TODO: use name to look up strategy
            return new TradingStrategy("1/3 on each RR level", TradingStrategyRRLevels.Run);
        }
    }
}