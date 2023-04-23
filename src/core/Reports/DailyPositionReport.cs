using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;

namespace core.Reports
{
    public class DailyPositionReport
    {
        public class Query : RequestWithUserId<DailyPositionReportView>
        {
            public Query(string ticker, int positionId, Guid userId) : base(userId)
            {
                Ticker = ticker;
                PositionId = positionId;
            }

            public string Ticker { get; }
            public int PositionId { get; }
        }

        public class Handler : HandlerWithStorage<Query, DailyPositionReportView>
        {
            public Handler(
                IAccountStorage accountStorage,
                IBrokerage brokerage,
                IMarketHours marketHours,
                IPortfolioStorage storage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
                _marketHours = marketHours;
            }

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }
            private IMarketHours _marketHours;

            public override async Task<DailyPositionReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stock = await _storage.GetStock(ticker: request.Ticker, userId: request.UserId);
                if (stock == null)
                {
                    throw new Exception("Stock not found");
                }

                var position = stock.State.GetPosition(request.PositionId);
                if (position == null)
                {
                    throw new Exception("Position not found");
                }

                var start = _marketHours.GetMarketStartOfDayTimeInUtc(
                    position.Opened.Value
                );

                var end = position.Closed switch {
                    null => _marketHours.GetMarketEndOfDayTimeInUtc(position.Closed.Value),
                    not null => default
                };

                var priceResponse = await _brokerage.GetPriceHistory(
                    user.State,
                    request.Ticker,
                    frequency: PriceFrequency.Daily,
                    start: start,
                    end: end);
                    
                if (!priceResponse.IsOk)
                {
                    throw new Exception("Failed to get price history: " + priceResponse.Error.Message);
                }

                var bars = priceResponse.Success;

                var (pl, gainPct) = PositionDailyPLAndGain.Generate(
                    bars,
                    position);

                return new DailyPositionReportView(pl, gainPct, position.Ticker);
            }
        }
    }
}