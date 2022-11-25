using System;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyRRLevels
    {
        public const string StrategyNameOneThirdRR = "1/3 on each RR level";
        public const string StrategyNameOneFourthRR = "1/4 on each RR level";

        public static TradingStrategyResult RunOneThirdRR(string name, PositionInstance positionInstance, PriceBar[] success)
            => Run(name, positionInstance, success, 3);

        public static TradingStrategyResult RunOneFourthRR(string name, PositionInstance positionInstance, PriceBar[] success)
            => Run(name, positionInstance, success, 4);

        public static TradingStrategyResult Run(string name, PositionInstance position, PriceBar[] prices, int rrLevels)
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

                if (!levelSells[currentLevel] && bar.High >= position.GetRRLevel(currentLevel))
                {
                    position.Sell(sellPortions[currentLevel], position.GetRRLevel(currentLevel).Value, Guid.NewGuid(), bar.Date);
                    
                    var stopPrice = currentLevel switch {
                        0 => position.AverageCostPerShare,
                        _ => position.GetRRLevel(currentLevel - 1).Value
                    };
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
            }

            return new TradingStrategyResult(
                maxGainPct: Math.Round(maxGain * 100, 2),
                maxDrawdownPct: Math.Round(maxDrawdown * 100, 2),
                position: position,
                strategyName: name
            );
        }
    }
}