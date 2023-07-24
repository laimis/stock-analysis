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

        public Task<TradingStrategyResults> RunAsync(
            UserState user,
            PositionInstance position,
            bool closeIfOpenAtTheEnd = false
        )
        {
            return RunAsync(
                user: user,
                numberOfShares: position.CompletedPositionShares,
                price: position.CompletedPositionCostPerShare,
                stopPrice: position.FirstStop.HasValue ?
                    position.FirstStop.Value
                    : position.CompletedPositionCostPerShare * TradingStrategyConstants.DEFAULT_STOP_PRICE_MULTIPLIER,
                ticker: position.Ticker,
                when: position.Opened.Value,
                closeIfOpenAtTheEnd: closeIfOpenAtTheEnd,
                actualTrade: position);
        }

        public Task<TradingStrategyResults> RunAsync(
            UserState user,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            bool closeIfOpenAtTheEnd = false)
        {
            return RunAsync(
                user,
                numberOfShares,
                price,
                stopPrice,
                ticker,
                when,
                closeIfOpenAtTheEnd,
                actualTrade: null);
        }

        private async Task<TradingStrategyResults> RunAsync(
            UserState user,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string ticker,
            DateTimeOffset when,
            bool closeIfOpenAtTheEnd = false,
            PositionInstance actualTrade = null)
        {
            // when we simulate a purchase for that day, assume it's end of the day
            // so that the price feed will return data from that day and not the previous one
            var convertedWhen = _hours.GetMarketEndOfDayTimeInUtc(when);

            var prices = await _brokerage.GetPriceHistory(
                user,
                ticker,
                Shared.Adapters.Stocks.PriceFrequency.Daily,
                convertedWhen,
                convertedWhen.AddDays(TradingStrategyConstants.MAX_NUMBER_OF_DAYS_TO_SIMULATE));
            
            var results = new TradingStrategyResults();

            if (!prices.IsOk)
            {
                results.MarkAsFailed($"Failed to get price history for {ticker}: " + prices.Error.Message);
                return results;
            }

            var bars = prices.Success;
            if (bars.Length == 0)
            {
                results.MarkAsFailed($"No price history found for {ticker}");
                return results;
            }

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

                var result = strategy.Run(
                    position: positionInstance,
                    bars: bars
                );

                if (closeIfOpenAtTheEnd && !result.position.IsClosed)
                {
                    result.position.Sell(
                        numberOfShares: result.position.NumberOfShares,
                        price: bars.Last().Close,
                        transactionId: Guid.NewGuid(),
                        when: bars.Last().Date
                    );
                }

                results.Add(result);
            }

            if (actualTrade != null)
            {
                var actualResult = TradingStrategyFactory.CreateActualTrade().Run(actualTrade, bars);
                results.Insert(0, actualResult);
            }

            return results;
        }
    }
}