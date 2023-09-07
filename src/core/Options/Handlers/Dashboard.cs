using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

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

        public class Handler : HandlerWithStorage<Query, OptionDashboardView>
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
                
                var options = await _storage.GetOwnedOptions(user.Id);

                var closedOptions = options
                    .Where(o => o.State.Closed != null)
                    .Select(o => o.State)
                    .OrderByDescending(o => o.FirstFill)
                    .ToList();

                var openOptions = new List<OwnedOptionView>();
                foreach(var o in options.Where(o => o.State.Closed == null).Select(o => o.State).OrderBy(o => o.Ticker).ThenBy(o => o.Expiration))
                {
                    var chain = await _brokerage.GetOptions(user.State, o.Ticker, expirationDate: o.Expiration, strikePrice: o.StrikePrice, contractType: o.OptionType.ToString());
                    var detail = chain.Success?.FindMatchingOption(strikePrice: o.StrikePrice, expirationDate: o.ExpirationDate, optionType: o.OptionType);
                    openOptions.Add(new OwnedOptionView(o, detail));
                }

                var brokerageAccount = await _brokerage.GetAccount(user.State);
                var positions = brokerageAccount.Success?.OptionPositions?.Where(
                    bp => !openOptions.Any(o => o.Ticker == bp.Ticker && o.StrikePrice == bp.StrikePrice && o.OptionType == bp.OptionType)
                ) ?? Array.Empty<OptionPosition>();

                var brokerageOrders = (brokerageAccount.Success?.Orders ?? Array.Empty<Order>()).Where(o => o.IsOption);

                return new OptionDashboardView(
                    closed: closedOptions.Select(o => new OwnedOptionView(o, optionDetail: null)),
                    open: openOptions,
                    brokeragePositions: positions,
                    orders: brokerageOrders
                );

            }
        }
    }
}