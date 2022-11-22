using System;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public class TradeStrategyRunner
    {
        private IBrokerage _brokerage;

        public TradeStrategyRunner(IBrokerage brokerage)
        {
            _brokerage = brokerage;
        }

        public async Task<PositionInstance> RunAsync(
            UserState user,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when)
        {
            var positionInstance = new PositionInstance(ticker);

            positionInstance.Buy(numberOfShares, price, when, Guid.NewGuid());
            positionInstance.SetStopPrice(stopPrice, when);

            var prices = await _brokerage.GetPriceHistory(user, ticker, Shared.Adapters.Stocks.PriceFrequency.Daily, when, when.AddDays(365));
            if (!prices.IsOk)
            {
                throw new Exception("Failed to get price history");
            }

            return RunWithStopInternal(
                positionInstance,
                prices.Success,
                positionInstance.StopPrice.Value
            );
        }

        private PositionInstance RunWithStopInternal(PositionInstance position, PriceBar[] prices, decimal stopPrice)
        {
            bool r1SellHappened = false, r2SellHappened = false;
            var sellPortion = (int)position.NumberOfShares / 3;

            foreach(var bar in prices)
            {
                // if stop is reached, sell at the close price
                if (bar.Close <= stopPrice)
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