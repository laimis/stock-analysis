using System;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Trading
{
    internal class TradingStrategyRRLevels
    {
        public static PositionInstance Run(PositionInstance position, PriceBar[] prices)
        {
            if (position.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            bool r1SellHappened = false, r2SellHappened = false;
            var sellPortion = (int)position.NumberOfShares / 3;
            // for positions with less than 3 shares, selling portion
            // is a minimum that can be sold, ie 1
            if (sellPortion == 0)
            {
                sellPortion = 1;
            }

            foreach(var bar in prices)
            {
                if (position.IsClosed)
                {
                    break;
                }
                
                // if stop is reached, sell at the close price
                if (bar.Close <= position.StopPrice.Value)
                {
                    position.Sell(position.NumberOfShares, bar.Close, Guid.NewGuid(), bar.Date);
                    break;
                }

                if (!r1SellHappened && bar.High > position.GetRRLevel(0))
                {
                    position.Sell(sellPortion, position.GetRRLevel(0).Value, Guid.NewGuid(), bar.Date);
                    position.SetStopPrice(position.AverageCostPerShare, bar.Date);
                    r1SellHappened = true;
                }

                if (!r2SellHappened && bar.High > position.GetRRLevel(1))
                {
                    position.Sell(sellPortion, position.GetRRLevel(1).Value, Guid.NewGuid(), bar.Date);
                    position.SetStopPrice(position.GetRRLevel(0).Value, bar.Date);
                    r2SellHappened = true;
                }

                if (r1SellHappened && r2SellHappened && bar.High > position.GetRRLevel(2))
                {
                    position.Sell(position.NumberOfShares, position.GetRRLevel(2).Value, Guid.NewGuid(), bar.Date);
                    break;
                }
            }

            return position;
        }
    }
}