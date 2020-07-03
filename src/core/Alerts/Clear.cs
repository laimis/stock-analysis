using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Alerts
{
    public class Clear
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
                await _alertsStorage.Delete(cmd.UserId);
                
                return new object();
            }
        }
    }
}