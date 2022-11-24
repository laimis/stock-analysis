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
            return new TradingStrategy("1/3 on each RR level", OneThirdOnEachRRLevel.Run);
        }
    }

    internal class OneThirdOnEachRRLevel
    {
        public static PositionInstance Run(PositionInstance position, PriceBar[] prices)
        {
            if (position.StopPrice == null)
            {
                throw new InvalidOperationException("Stop price is not set");
            }

            bool r1SellHappened = false, r2SellHappened = false;
            var sellPortion = (int)position.NumberOfShares / 3;

            foreach(var bar in prices)
            {
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

            if (!position.IsClosed)
            {
                position.Sell(position.NumberOfShares, prices[prices.Length - 1].Close, Guid.NewGuid(), prices[prices.Length - 1].Date);
            }

            return position;
        }
    }
}