using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class Create
    {
        public class Command : RequestWithUserId<object>
        {
            public string Description { get; set; }

            [Required]
            public string Ticker { get; set; }

            [Required]
            [Range(0.01, 10000)]
            public double Value { get; set; }
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

                var added = alert.AddPricePoint(cmd.Description, cmd.Value);
                if (!added)
                {
                    throw new InvalidOperationException("Alert for the price point already exists");
                }

                await _alertsStorage.Save(alert);

                return new {
                    ticker = alert.State.Ticker.Value,
                    points = alert.State.PricePoints
                };
            }
        }
    }
}