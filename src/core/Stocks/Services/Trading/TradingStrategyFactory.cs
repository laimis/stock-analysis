using System.Collections.Generic;

namespace core.Stocks.Services.Trading
{
    public class TradingStrategyFactory
    {
        public static IEnumerable<ITradingStrategy> GetStrategies()
        {
            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneThirdRR
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

            yield return new TradingStrategy(
                TradingStrategyRRLevels.RunOneThirdRRDelayedStop
            );
        }
    }
}