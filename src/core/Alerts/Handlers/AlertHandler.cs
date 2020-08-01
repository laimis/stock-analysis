using System.Threading;
using System.Threading.Tasks;

namespace core.Alerts
{
    public class AlertHandler :
        MediatR.INotificationHandler<AlertPricePointRemoved>
    {
        private StockMonitorContainer _container;
        private IAlertsStorage _storage;

        public AlertHandler(IAlertsStorage storage, StockMonitorContainer container)
        {
            _container = container;
            _storage = storage;
        }

        public Task Handle(AlertPricePointRemoved e, CancellationToken cancellationToken)
        {
            _container.Deregister(e.PricePointId);

            return Task.CompletedTask;
        }
    }
}