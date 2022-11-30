using System;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyRRLevels
    {
        private const decimal AVG_PERCENT_GAIN = 0.07m;

        private static Func<int, PositionInstance, decimal> _alwaysPositionStop = (_, position) => position.StopPrice.Value;
        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _advancingStop = (level, position, rrLevelFunc) => level switch {
                        0 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 1)
                    };

        private static Func<int, PositionInstance, Func<int, decimal>, decimal> _delayedAdvancingStop = (level, position, rrLevelFunc) => level switch {
                        0 => position.StopPrice.Value,
                        1 => position.AverageCostPerShare,
                        _ => rrLevelFunc(level - 2)
                    };
        
        public static TradingStrategyResult RunOneThirdRR(
            PositionInstance positionInstance,
            PriceBar[] success,
            bool closeIfOpenAtTheEnd)
            => Run(
                "1/3 on each RR level",
                positionInstance,
                success,
                3,
                level => positionInstance.GetRRLevel(level).Value,
                (level, position) => _advancingStop(level, position, ( l ) => position.GetRRLevel(l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneThirdRRDelayedStop(
            PositionInstance positionInstance,
            PriceBar[] success,
            bool closeIfOpenAtTheEnd)
            => Run(
                "1/3 on each RR level (delayed stop)",
                positionInstance,
                success,
                3,
                level => positionInstance.GetRRLevel(level).Value,
                (level, position) => _delayedAdvancingStop(level, position, ( l ) => position.GetRRLevel(l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneFourthRR(
            PositionInstance positionInstance,
            PriceBar[] success,
            bool closeIfOpenAtTheEnd)
            => Run(
                "1/4 on each RR level",
                positionInstance,
                success,
                4,
                level => positionInstance.GetRRLevel(level).Value,
                (level, position) => _advancingStop(level, position, ( l ) => position.GetRRLevel(l).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneThirdPercentBased(
            PositionInstance positionInstance,
            PriceBar[] success,
            bool closeIfOpenAtTheEnd)
            => Run(
                "1/3 on each RR level (percent based)",
                positionInstance,
                success,
                3,
                level => positionInstance.GetRRLevelPercentBased(level, AVG_PERCENT_GAIN).Value,
                (level, position) => _advancingStop(level, position, ( l ) => position.GetRRLevelPercentBased(l, AVG_PERCENT_GAIN).Value),
                closeIfOpenAtTheEnd);

        public static TradingStrategyResult RunOneFourthPercentBased(
            PositionInstance positionInstance,
            PriceBar[] success,
            bool closeIfOpenAtTheEnd)
            => Run(
                "1/4 on each RR level (percent based)",
                positionInstance,
                success,
                4,
                level => positionInstance.GetRRLevelPercentBased(level, AVG_PERCENT_GAIN).Value,
                (level, position) => _advancingStop(level, position, ( l ) => position.GetRRLevelPercentBased(l, AVG_PERCENT_GAIN).Value),
                closeIfOpenAtTheEnd);

        private static TradingStrategyResult Run(
            string name,
            PositionInstance position,
            PriceBar[] prices,
            int rrLevels,
            Func<int, decimal> getRRLevelFunc,
            Func<int, PositionInstance, decimal> getStopPriceFunc,
            bool closeIfOpenAtTheEnd)
        {
            if (position.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            var maxGain = 0m;
            var maxDrawdown = 0m;

            var levelSells = new bool[rrLevels];
            var currentLevel = 0;
            var multiplier = (int)position.NumberOfShares / rrLevels;
            if (multiplier == 0)
            {
                // position size too small to split into portions, so just sell 1 share
                // at a time
                multiplier = 1;
            }
            var sellPortions = new int[rrLevels];
            for(var i = 0; i < sellPortions.Length; i++)
            {
                sellPortions[i] = multiplier;
            }
            sellPortions[^1] += (int)position.NumberOfShares % rrLevels;
            
            foreach(var bar in prices)
            {
                if (position.IsClosed)
                {
                    break;
                }

                // check if it's the max gain
                var gain = (bar.High - position.AverageBuyCostPerShare) / position.AverageBuyCostPerShare;
                if (gain > maxGain)
                {
                    maxGain = gain;
                }

                var loss = (bar.Low - position.AverageBuyCostPerShare) / position.AverageBuyCostPerShare;
                if (loss < maxDrawdown)
                {
                    maxDrawdown = loss;
                }
                
                // if stop is reached, sell at the close price
                if (bar.Close <= position.StopPrice.Value)
                {
                    position.Sell(position.NumberOfShares, bar.Close, Guid.NewGuid(), bar.Date);
                    break;
                }

                if (!levelSells[currentLevel] && bar.High >= getRRLevelFunc(currentLevel))
                {
                    position.Sell(sellPortions[currentLevel], getRRLevelFunc(currentLevel), Guid.NewGuid(), bar.Date);
                    
                    var stopPrice = getStopPriceFunc(currentLevel, position);
                    
                    position.SetStopPrice(stopPrice, bar.Date);
                    levelSells[currentLevel] = true;
                    currentLevel++;
                }
            }

            if (!position.IsClosed)
            {
                // if the position is still open, let's set the latest price we had for it
                // so that we can render unrealized stats
                position.SetPrice(prices[^1].Close);

                if (closeIfOpenAtTheEnd)
                {
                    position.Sell(position.NumberOfShares, prices[^1].Close, Guid.NewGuid(), prices[^1].Date);
                }
            }

            return new TradingStrategyResult(
                maxGainPct: maxGain,
                maxDrawdownPct: maxDrawdown,
                position: position,
                strategyName: name
            );
        }
    }
}