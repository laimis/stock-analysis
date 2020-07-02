using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;

namespace core.Alerts
{
    public class AlertClearedHandler : MediatR.INotificationHandler<AlertCleared>
    {
        private IAlertsStorage _storage;

        public AlertClearedHandler(IAlertsStorage storage, IEmailService email)
        {
            _storage = storage;
        }

        public async Task Handle(AlertCleared e, CancellationToken cancellationToken)
        {
            var a = await _storage.GetAlert(e.AggregateId, e.UserId);
            if (a == null)
            {
                return;
            }
            await _storage.Delete(a);
        }
    }
}