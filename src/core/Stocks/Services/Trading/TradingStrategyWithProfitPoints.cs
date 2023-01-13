using System;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyWithProfitPoints
    {
        public static TradingStrategyResult Run(
            string name,
            PositionInstance position,
            PriceBar[] prices,
            int numberOfProfitPoints,
            Func<int, decimal> getProfitPointFunc,
            Func<int, PositionInstance, decimal> getStopPriceFunc,
            bool closeIfOpenAtTheEnd,
            bool downsideProtectionEnabled = false)
        {
            if (position.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            var maxGain = 0m;
            var maxDrawdown = 0m;
            var totalShares = position.NumberOfShares;
            var downsideProtectionExecuted = false;

            var currentLevel = 1;
            var currentProfitPoint = getProfitPointFunc(currentLevel);

            foreach(var bar in prices)
            {
                if (position.IsClosed)
                {
                    break;
                }

                // check if it's the max gain
                maxGain = AdjustMaxGainIfNecessary(position, maxGain, bar);
                
                maxDrawdown = AdjustMaxDrawDownIfNecessary(position, maxDrawdown, bar);

                if (bar.High >= currentProfitPoint)
                {
                    ExecuteSell(
                        position,
                        numberOfProfitPoints,
                        totalShares,
                        currentLevel,
                        currentProfitPoint,
                        bar
                    );

                    if (position.NumberOfShares > 0)
                    {
                        var stopPrice = getStopPriceFunc(currentLevel, position);
                        position.SetStopPrice(stopPrice, bar.Date);
                    }

                    currentLevel++;
                    currentProfitPoint = getProfitPointFunc(currentLevel);
                }

                // if stop is reached, sell at the close price
                if (bar.Close <= position.StopPrice.Value)
                {
                    position.Sell(position.NumberOfShares, bar.Close, Guid.NewGuid(), bar.Date);
                }
                
                position.SetPrice(bar.Close);

                // if our r/r ratio goes past  -0.5 for the first time, let's sell half of the position
                if (downsideProtectionEnabled && !downsideProtectionExecuted && position.RR < -0.5m && position.NumberOfShares > 0)
                {
                    var stocksToSell = (int)position.NumberOfShares / 2;
                    if (stocksToSell > 0)
                    {
                        position.Sell(stocksToSell, bar.Close, Guid.NewGuid(), bar.Date);
                        downsideProtectionExecuted = true;
                    }
                }
            }

            if (!position.IsClosed && closeIfOpenAtTheEnd)
            {
                position.Sell(position.NumberOfShares, prices[^1].Close, Guid.NewGuid(), prices[^1].Date);
            }

            return new TradingStrategyResult(
                maxGainPct: maxGain,
                maxDrawdownPct: maxDrawdown,
                position: position,
                strategyName: name
            );
        }

        private static void ExecuteSell(PositionInstance position, int numberOfProfitPoints, decimal totalShares, int currentLevel, decimal currentProfitPoint, PriceBar bar)
        {
            var portion = (int)totalShares / numberOfProfitPoints;
            if (portion == 0)
            {
                portion = 1;
            }

            if (position.NumberOfShares < portion)
            {
                portion = (int)position.NumberOfShares;
            }

            if (currentLevel == numberOfProfitPoints)
            {
                // sell all the remaining shares
                portion = (int)position.NumberOfShares;
            }

            position.Sell(portion, currentProfitPoint, Guid.NewGuid(), bar.Date);
        }

        private static decimal AdjustMaxDrawDownIfNecessary(PositionInstance position, decimal maxDrawdown, PriceBar bar)
        {
            var loss = (bar.Low - position.AverageBuyCostPerShare) / position.AverageBuyCostPerShare;
            if (loss < maxDrawdown)
            {
                maxDrawdown = loss;
            }

            return maxDrawdown;
        }

        private static decimal AdjustMaxGainIfNecessary(PositionInstance position, decimal maxGain, PriceBar bar)
        {
            var gain = (bar.High - position.AverageBuyCostPerShare) / position.AverageBuyCostPerShare;
            if (gain > maxGain)
            {
                maxGain = gain;
            }

            return maxGain;
        }
    }
}