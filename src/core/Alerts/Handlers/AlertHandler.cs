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
        private StockMonitorContainer _container;
        private IPortfolioStorage _storage;

        public AlertHandler(IPortfolioStorage storage, StockMonitorContainer container)
        {
            _container = container;
            _storage = storage;
        }

        public async Task Handle(StockPurchased_v2 notification, CancellationToken cancellationToken)
        {
            var stock = await _storage.GetStock(ticker: notification.Ticker, userId: notification.UserId);
            if (stock == null)
            {
                return;
            }

            if (stock.State.OpenPosition == null)
            {
                return;
            }

            _container.Register(stock);
        }

        public async Task Handle(StockSold notification, CancellationToken cancellationToken)
        {
            var stock = await _storage.GetStock(ticker: notification.Ticker, userId: notification.UserId);
            if (stock == null)
            {
                return;
            }

            if (stock.State.OpenPosition != null)
            {
                return;
            }

            _container.Deregister(stock);
        }

        public async Task Handle(StopPriceSet notification, CancellationToken cancellationToken)
        {
            var stock = await _storage.GetStock(ticker: notification.Ticker, userId: notification.UserId);
            if (stock == null)
            {
                return;
            }

            if (stock.State.OpenPosition == null)
            {
                return;
            }

            _container.Register(stock);
        }


        public async Task Handle(StopDeleted notification, CancellationToken cancellationToken)
        {
            var stock = await _storage.GetStock(ticker: notification.Ticker, userId: notification.UserId);
            if (stock == null)
            {
                return;
            }

            if (stock.State.OpenPosition == null)
            {
                return;
            }

            _container.Deregister(stock);
        }
    }
}