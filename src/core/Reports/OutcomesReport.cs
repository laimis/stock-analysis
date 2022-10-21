using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;

namespace core.Reports
{
    public class OutcomesReport
    {
        public class ForPortfolioQuery : RequestWithUserId<List<TickerOutcomes>>
        {
            public ForPortfolioQuery(PriceFrequency frequency, Guid userId) : base(userId)
            {
                Frequency = frequency;
            }

            public PriceFrequency Frequency { get; }
        }

        public class DailyQuery : RequestWithUserId<List<TickerOutcomes>>
        {
            public DailyQuery(Guid userId) : base(userId){}
        }

        public class Handler : HandlerWithStorage<ForPortfolioQuery, List<TickerOutcomes>>
        {
            public Handler(
                IAccountStorage accountStorage,
                IPortfolioStorage storage,
                IBrokerage brokerage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
            }

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }

            public override async Task<List<TickerOutcomes>> Handle(ForPortfolioQuery request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(request.UserId);

                var tickers = stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.Ticker).ToList();

                var func = request.Frequency switch
                {
                    PriceFrequency.Daily => (Func<HistoricalPrice[], List<AnalysisOutcome>>) (prices => HistoricalPriceAnalysis.Run(
                                currentPrice: prices[prices.Length - 1].Close,
                                prices
                            )),
                    PriceFrequency.Weekly => prices => SingleBarAnalysisRunner.Run(prices),
                    _ => throw new ArgumentOutOfRangeException()
                };


                return await RunAnalysis(
                    tickers,
                    user.State,
                    func
                );
            }

            private async Task<List<TickerOutcomes>> RunAnalysis(List<string> tickers, UserState user, Func<HistoricalPrice[], List<AnalysisOutcome>> func)
            {
                var list = new List<TickerOutcomes>();

                foreach(var ticker in tickers)
                {
                    var historicalResponse = await _brokerage.GetHistoricalPrices(user, ticker);

                    var outcomes = func(historicalResponse.Success);

                    list.Add(new TickerOutcomes(outcomes, ticker));
                }

                return list;
            }
        }
    }
}