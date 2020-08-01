using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class Migrate
    {
        public class Command : RequestWithUserId<object>
        {
            public Command(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Command, object>
        {
            private IAlertsStorage _alertsStorage;

            public Handler(
                IPortfolioStorage storage,
                IAlertsStorage alertsStorage) : base(storage)
            {
                _alertsStorage = alertsStorage;
            }

            public override async Task<object> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var owned = await _storage.GetStocks(cmd.UserId);

                var alerts = await _alertsStorage.GetAlerts(cmd.UserId);
                
                foreach(var o in owned)
                {
                    if (o.State.Owned == 0)
                    {
                        continue;
                    }

                    var exists = alerts.Any(a => a.State.Ticker == o.State.Ticker);
                    if (!exists)
                    {
                        var a = new Alert(o.State.Ticker, cmd.UserId);

                        a.AddPricePoint("Migrated average cost", o.State.AverageCost);

                        await _alertsStorage.Save(a);
                    }
                }

                return new object();
            }
        }
    }
}