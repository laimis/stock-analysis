

using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Tracking.Handlers
{
    public class Register
    {
        public class Command : RequestWithUserId<object>
        {
            public Command(string ticker) => Ticker = ticker;

            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Command, object>
        {
            private IStocksService2 _stocksService;

            public Handler(IPortfolioStorage storage, IStocksService2 stockService) : base(storage)
            {
                _stocksService = stockService;
            }

            public override async Task<object> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var c = await _stocksService.GetCompanyProfile(cmd.Ticker);
                var a = await _stocksService.GetAdvancedStats(cmd.Ticker);
                var q = await _stocksService.Quote(cmd.Ticker);

                return new RegisterPreview(c, a, q);
            }
        }
    }
}