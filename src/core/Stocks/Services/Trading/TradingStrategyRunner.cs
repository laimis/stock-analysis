using System;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.Services.Trading
{
    public class TradingStrategyRunner
    {
        private IBrokerage _brokerage;

        public TradingStrategyRunner(IBrokerage brokerage)
            => _brokerage = brokerage;

        public async Task<TradingStrategyResults> RunAsync(
            UserState user,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when)
        {
            var prices = await _brokerage.GetPriceHistory(
                user,
                ticker,
                Shared.Adapters.Stocks.PriceFrequency.Daily,
                when,
                when.AddDays(365));
            
            if (!prices.IsOk)
            {
                throw new Exception("Failed to get price history");
            }

            var results = new TradingStrategyResults();

            foreach(var strategy in TradingStrategyFactory.GetStrategies())
            {
                var positionInstance = new PositionInstance(0, ticker);

                positionInstance.Buy(numberOfShares, price, when, Guid.NewGuid());
                positionInstance.SetStopPrice(stopPrice, when);

                var result = strategy.Run(positionInstance, prices.Success);
                results.Results.Add(result);
            }

            return results;
        }
    }
}