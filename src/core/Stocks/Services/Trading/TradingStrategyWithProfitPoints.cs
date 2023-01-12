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
            bool closeIfOpenAtTheEnd)
        {
            if (position.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            var maxGain = 0m;
            var maxDrawdown = 0m;
            var totalShares = position.NumberOfShares;

            var currentLevel = 1;
            var currentProfitPoint = getProfitPointFunc(currentLevel);

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

                if (bar.High >= currentProfitPoint)
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
                    break;
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