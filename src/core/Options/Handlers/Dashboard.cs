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
        public class Query : RequestWithUserId<OptionDashboardView>
        {
            public Query(Guid userId) :base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, OptionDashboardView>,
            INotificationHandler<UserChanged>
        {
            private IStocksService2 _stockService;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService) : base(storage)
            {
                _stockService = stockService;
            }

            public override async Task<OptionDashboardView> Handle(Query request, CancellationToken cancellationToken)
            {
                var view = await _storage.ViewModel<OptionDashboardView>(request.UserId);
                if (view == null)
                {
                    view = await FromDb(request.UserId);
                }

                var prices = await _stockService.GetPrices(
                    view.OpenOptions.Select(o => o.Ticker).ToList()
                );

                foreach (var op in view.OpenOptions)
                {
                    prices.TryGetValue(op.Ticker, out var val);
                    if (val != null) op.ApplyPrice(val.Price);
                }

                return view;
            }

            public async Task Handle(UserChanged notification, CancellationToken cancellationToken)
            {
                var view = await FromDb(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, view);
            }

            private async Task<OptionDashboardView> FromDb(Guid userId)
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

                var view = new OptionDashboardView(
                    closedOptions.Select(o => new OwnedOptionView(o)),
                    openOptions.Select(o => new Options.OwnedOptionView(o))
                );
                return view;
            }
        }
    }
}