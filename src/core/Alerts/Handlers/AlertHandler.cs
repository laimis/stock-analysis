using System.Threading;
using System.Threading.Tasks;
using core.Stocks;

namespace core.Alerts
{
    // NOTE: implementations are currently empty on purpose. Previously
    // we would update alerts container immediately, but with some changes
    // to the monitoring model, there is no need to do so. we might go back
    // to the model where these updates are needed, so I left the stubs in place
    public class AlertHandler :
        MediatR.INotificationHandler<StockPurchased_v2>,
        MediatR.INotificationHandler<StockSold>,
        MediatR.INotificationHandler<StopPriceSet>,
        MediatR.INotificationHandler<StopDeleted>
    {
        private StockAlertContainer _container;
        private IPortfolioStorage _storage;

        public AlertHandler(IPortfolioStorage storage, StockAlertContainer container)
        {
            _container = container;
            _storage = storage;
        }

        public Task Handle(StockPurchased_v2 notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Handle(StockSold notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Handle(StopPriceSet notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task Handle(StopDeleted notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}