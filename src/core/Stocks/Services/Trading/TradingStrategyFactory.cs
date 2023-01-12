using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    public class TradingStrategyFactory
    {
        public static IEnumerable<ITradingStrategy> GetStrategies()
        {
            yield return new TradingStrategy(
                RunOneThirdRR
            );

            yield return new TradingStrategy(
                RunOneFourthRR
            );

            yield return new TradingStrategy(
                RunOneThirdPercentBased
            );

            yield return new TradingStrategy(
                RunOneFourthPercentBased
            );

            yield return new TradingStrategy(
                RunOneThirdRRDelayedStop
            );
        }

        private static Func<int, PositionInstance, decimal> _alwaysPositionStop = (_, position) => position.StopPrice.Value;
        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _advancingStop = (level, position, rrLevelFunc) => level switch {
                        1 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 1)
                    };

        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _delayedAdvancingStop = (level, position, rrLevelFunc) => level switch {
                        1 => position.StopPrice.Value,
                        2 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 2)
                    };
        
        public static TradingStrategyResult RunOneThirdRR(
            PositionInstance positionInstance,
            PriceBar[] prices,
            bool closeIfOpenAtTheEnd)
            => TradingStrategyWithProfitPoints.Run(
                "1/3 on each RR level",
                positionInstance,
                prices,
                3,
                level => ProfitPoints.GetProfitPointWithStopPrice(positionInstance, level).Value,
                (level, position) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneThirdRRDelayedStop(
            PositionInstance positionInstance,
            PriceBar[] prices,
            bool closeIfOpenAtTheEnd)
            => TradingStrategyWithProfitPoints.Run(
                "1/3 on each RR level (delayed stop)",
                positionInstance,
                prices,
                3,
                level => ProfitPoints.GetProfitPointWithStopPrice(positionInstance, level).Value,
                (level, position) => _delayedAdvancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneFourthRR(
            PositionInstance positionInstance,
            PriceBar[] prices,
            bool closeIfOpenAtTheEnd)
            => TradingStrategyWithProfitPoints.Run(
                "1/4 on each RR level",
                positionInstance,
                prices,
                4,
                level => ProfitPoints.GetProfitPointWithStopPrice(positionInstance, level).Value,
                (level, position) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneThirdPercentBased(
            PositionInstance positionInstance,
            PriceBar[] prices,
            bool closeIfOpenAtTheEnd)
            => TradingStrategyWithProfitPoints.Run(
                "1/3 on each RR level (percent based)",
                positionInstance,
                prices,
                3,
                level => ProfitPoints.GetProfitPointWithPercentGain(positionInstance, level, TradingStrategyConstants.AVG_PERCENT_GAIN).Value,
                (level, position) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithPercentGain(position, l, TradingStrategyConstants.AVG_PERCENT_GAIN).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneFourthPercentBased(
            PositionInstance positionInstance,
            PriceBar[] prices,
            bool closeIfOpenAtTheEnd)
            => TradingStrategyWithProfitPoints.Run(
                "1/4 on each RR level (percent based)",
                positionInstance,
                prices,
                4,
                level => ProfitPoints.GetProfitPointWithPercentGain(positionInstance, level, TradingStrategyConstants.AVG_PERCENT_GAIN).Value,
                (level, position) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithPercentGain(position, l, TradingStrategyConstants.AVG_PERCENT_GAIN).Value),
                closeIfOpenAtTheEnd);
    }
}