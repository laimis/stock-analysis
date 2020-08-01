using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class Delete
    {
        public class Command : RequestWithUserId<object>
        {
            [Required]
            public string Ticker { get; set; }

            [Required]
            public Guid? Id { get; set; }
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
                var alert = await _alertsStorage.GetAlert(cmd.Ticker, cmd.UserId);

                if (alert == null)
                {
                    alert = new Alert(cmd.Ticker, cmd.UserId);
                }

                alert.RemovePricePoint(cmd.Id.Value);

                await _alertsStorage.Save(alert);

                return new {
                    ticker = alert.State.Ticker.Value,
                    points = alert.State.PricePoints
                };
            }
        }
    }
}