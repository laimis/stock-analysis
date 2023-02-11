using System;
using System.Threading;
using System.Threading.Tasks;
using core.Stocks;

namespace core.Alerts
{
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
            return RequestManualRun();
        }

        private Task RequestManualRun()
        {
            _container.RequestManualRun();

            return Task.CompletedTask;
        }

        public Task Handle(StockSold notification, CancellationToken cancellationToken)
        {
            return RemoveStopAlerts(notification.Ticker, notification.UserId);
        }

        public Task Handle(StopPriceSet notification, CancellationToken cancellationToken)
        {
            return RemoveStopAlerts(notification.Ticker, notification.UserId);
        }

        public Task Handle(StopDeleted notification, CancellationToken cancellationToken)
        {
            return RemoveStopAlerts(notification.Ticker, notification.UserId);
        }

        private Task RemoveStopAlerts(string ticker, Guid userId)
        {
            StopPriceMonitor.Deregister(_container, ticker, userId);
            return Task.CompletedTask;
        }
    }
}