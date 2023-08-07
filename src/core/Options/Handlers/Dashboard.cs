using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
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
            private readonly IAccountStorage _accounts;
            private readonly IBrokerage _brokerage;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage storage
                ) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;    
            }

            public override async Task<OptionDashboardView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId)
                    ?? throw new Exception("User not found");
                var view = await _storage.ViewModel<OptionDashboardView>(request.UserId, OptionDashboardView.Version);
                view ??= await FromDb(user.State);

                var prices = await _brokerage.GetQuotes(
                    user.State,
                    view.Open.Select(o => o.Ticker).ToList()
                );

                return prices.IsOk switch {
                    false => view,
                    true => EnrichWithStockPrice(view, prices.Success)
                };
            }

            private static OptionDashboardView EnrichWithStockPrice(OptionDashboardView view, Dictionary<string, StockQuote> prices)
            {
                foreach (var op in view.Open)
                {
                    prices.TryGetValue(op.Ticker, out var val);
                    if (val != null) op.ApplyPrice(val.Price);
                }
                return view;
            }

            public async Task Handle(UserChanged notification, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(notification.UserId)
                    ?? throw new Exception("User not found");
                var view = await FromDb(user.State);

                await _storage.SaveViewModel(notification.UserId, view, OptionDashboardView.Version);
            }

            private async Task<OptionDashboardView> FromDb(UserState user)
            {
                var options = await _storage.GetOwnedOptions(user.Id);
                options = options.Where(o => !o.State.Deleted);

                var openOptions = options
                    .Where(o => o.State.NumberOfContracts != 0 && o.State.DaysUntilExpiration > -5)
                    .Select(o => o.State)
                    .OrderBy(o => o.Expiration)
                    .ToList();

                var closedOptions = options
                    .Where(o => o.State.Closed != null)
                    .Select(o => o.State)
                    .OrderByDescending(o => o.FirstFill);

                var brokeragePositions = await _brokerage.GetAccount(user);
                var positions = brokeragePositions.Success?.OptionPositions?.Where(
                    bp => !openOptions.Any(o => o.Ticker == bp.Ticker && o.StrikePrice == bp.StrikePrice && o.OptionType.ToString() == bp.OptionType)
                ) ?? Array.Empty<OptionPosition>();

                var view = new OptionDashboardView(
                    closedOptions.Select(o => new OwnedOptionView(o)),
                    openOptions.Select(o => new Options.OwnedOptionView(o)),
                    positions
                );
                return view;
            }
        }
    }
}