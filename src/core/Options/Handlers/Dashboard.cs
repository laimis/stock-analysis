using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Dashboard
    {
        public class Query : RequestWithUserId<OwnedOptionStatsView>
        {
            public Query(Guid userId) :base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, OwnedOptionStatsView>,
            INotificationHandler<UserRecalculate>
        {
            private IStocksService2 _stockService;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService) : base(storage)
            {
                _stockService = stockService;
            }

            public override async Task<OwnedOptionStatsView> Handle(Query request, CancellationToken cancellationToken)
            {
                var view = await _storage.ViewModel<OwnedOptionStatsView>(request.UserId);
                if (view == null)
                {
                    view = await FromDb(request.UserId);
                }

                var prices = await _stockService.GetPrices(
                    view.OpenOptions.Select(o => o.Ticker)
                );

                foreach (var op in view.OpenOptions)
                {
                    prices.TryGetValue(op.Ticker, out var val);
                    op.ApplyPrice(val?.Price ?? 0);
                }

                return view;
            }

            public async Task Handle(UserRecalculate notification, CancellationToken cancellationToken)
            {
                var view = await FromDb(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, view);
            }

            private async Task<OwnedOptionStatsView> FromDb(Guid userId)
            {
                var options = await _storage.GetOwnedOptions(userId);
                options = options.Where(o => !o.State.Deleted);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .OrderBy(o => o.State.Expiration)
                    .ToList();

                var closedOptions = options
                    .Where(o => o.State.Closed != null)
                    .OrderByDescending(o => o.State.FirstFill);

                var view = new OwnedOptionStatsView(
                    closedOptions.Select(o => new OwnedOptionView(o)),
                    openOptions.Select(o => new Options.OwnedOptionView(o))
                );
                return view;
            }
        }
    }
}