using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;

namespace core.Notes
{
    public class NoteCreatedHandler : MediatR.INotificationHandler<NoteCreated>
    {
        private IPortfolioStorage _storage;
        private IStocksService2 _stocks;

        public NoteCreatedHandler(IPortfolioStorage storage, IStocksService2 stocks)
        {
            _storage = storage;
            _stocks = stocks;
        }

        public async Task Handle(NoteCreated e, CancellationToken cancellationToken)
        {
            var n = await _storage.GetNote(e.UserId, e.AggregateId);
            if (n == null)
            {
                return;
            }

            var d = await _stocks.GetAdvancedStats(n.State.RelatedToTicker);
            var p = await _stocks.GetPrice(n.State.RelatedToTicker);

            n.Enrich(p, d);

            await _storage.Save(n, n.State.UserId);
        }
    }
}