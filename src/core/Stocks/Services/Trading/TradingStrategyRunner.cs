using System;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.Services.Trading
{
    public class TradingStrategyRunner
    {
        private IBrokerage _brokerage;
        private IMarketHours _hours;

        public TradingStrategyRunner(IBrokerage brokerage, IMarketHours hours)
        {
            _brokerage = brokerage;
            _hours = hours;
        }

        public async Task<TradingStrategyResults> RunAsync(
            UserState user,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            bool closeIfOpenAtTheEnd = false)
        {
            // when we simulate a purchase for that day, assume it's end of the day
            // so that the price feed will return data from that day and not the previous one
            var convertedWhen = _hours.GetMarketEndOfDayTimeInUtc(when);

            var prices = await _brokerage.GetPriceHistory(
                user,
                ticker,
                Shared.Adapters.Stocks.PriceFrequency.Daily,
                convertedWhen,
                convertedWhen.AddDays(365));
            
            if (!prices.IsOk)
            {
                throw new Exception("Failed to get price history");
            }
            
            var results = new TradingStrategyResults();

            var bars = prices.Success;

            // HACK: sometimes stock is purchased in after hours at a much higher or lower price
            // than what the day's high/close was, we need to move the prices to the next day
            if (price > bars[0].High)
            {
                bars = bars.Skip(1).ToArray();
            }

            foreach(var strategy in TradingStrategyFactory.GetStrategies())
            {
                var positionInstance = new PositionInstance(0, ticker);

                positionInstance.Buy(numberOfShares, price, when, Guid.NewGuid());
                positionInstance.SetStopPrice(stopPrice, when);

                var result = strategy.Run(positionInstance, bars, closeIfOpenAtTheEnd);
                results.Results.Add(result);
            }

            return results;
        }
    }
}