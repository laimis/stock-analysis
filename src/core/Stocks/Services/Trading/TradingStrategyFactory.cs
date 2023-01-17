using System;
using System.Collections.Generic;

namespace core.Stocks.Services.Trading
{
    public class TradingStrategyFactory
    {
        internal static IEnumerable<ITradingStrategy> GetStrategies()
        {
            yield return CreateProfitTakingStrategy(
                name: "1/3 on each RR level"
            );

            yield return CreateProfitTakingStrategy(
                name: "1/4 on each RR level",
                profitPoints: 4
            );

            yield return new TradingStrategyWithProfitPoints(
                "1/3 on each RR level (percent based)",
                numberOfProfitPoints: 3,
                (position, level) => ProfitPoints.GetProfitPointWithPercentGain(position, level, TradingStrategyConstants.AVG_PERCENT_GAIN).Value,
                (position, level) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithPercentGain(position, l, TradingStrategyConstants.AVG_PERCENT_GAIN).Value)
            );

            yield return new TradingStrategyWithProfitPoints(
                "1/4 on each RR level (percent based)",
                numberOfProfitPoints: 4,
                (position, level) => ProfitPoints.GetProfitPointWithPercentGain(position, level, TradingStrategyConstants.AVG_PERCENT_GAIN).Value,
                (position, level) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithPercentGain(position, l, TradingStrategyConstants.AVG_PERCENT_GAIN).Value)
            );

            yield return new TradingStrategyWithProfitPoints(
                "1/3 on each RR level (delayed stop)",
                numberOfProfitPoints: 3,
                (position, level) => ProfitPoints.GetProfitPointWithStopPrice(position, level).Value,
                (position, level) => _delayedAdvancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value)
            );

            yield return CreateOneThirdRRWithDownsideProtection(2);

            yield return CreateOneThirdRRWithDownsideProtection(3);

            yield return CreateProfitTakingStrategy(
                "1/3 on each RR level (low as stop)",
                3,
                useLowAsStop: true
            );

            yield return new TradingStrategyRandomClose("Random close");
        }

        public static ITradingStrategy CreateProfitTakingStrategy(string name, int profitPoints = 3, bool useLowAsStop = false)
        {
            return new TradingStrategyWithProfitPoints(
                name: name,
                numberOfProfitPoints: profitPoints,
                (position, level) => ProfitPoints.GetProfitPointWithStopPrice(position, level).Value,
                (position, level) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value),
                useLowAsStop: useLowAsStop
            );
        }

        public static ITradingStrategy CreateOneThirdRRWithDownsideProtection(int downsideProtectionSize = 2)
        {
            return new TradingStrategyWithDownsideProtection(
                $"1/3 on each RR level (1/{downsideProtectionSize} downside protection)",
                numberOfProfitPoints: 3,
                (position, level) => ProfitPoints.GetProfitPointWithStopPrice(position, level).Value,
                (position, level) => _advancingStop(level, position, ( l ) => ProfitPoints.GetProfitPointWithStopPrice(position, l).Value),
                downsideProtectionSize
            );
        }

        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _advancingStop = (level, position, rrLevelFunc) => level switch {
                        1 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 1)
                    };

        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _delayedAdvancingStop = (level, position, rrLevelFunc) => level switch {
                        1 => position.StopPrice.Value,
                        2 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 2)
                    };
    }
}