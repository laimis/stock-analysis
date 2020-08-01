using System;
using System.Threading;
using System.Threading.Tasks;
using core.Portfolio.Output;
using core.Shared;

namespace core.Portfolio
{
    public class Transactions
    {
        public class Query : RequestWithUserId<TransactionList>
        {
            public Query(Guid userId, string ticker, string groupBy, string show, string txType) : base(userId)
            {
                this.Ticker = ticker;
                this.GroupBy = groupBy;
                this.Show = show;
                this.TxType = txType;
            }

            public string Ticker { get; }
            public string GroupBy { get; }
            public string Show { get; }
            public string TxType { get; }
        }

        public class Handler : HandlerWithStorage<Query, TransactionList>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<TransactionList> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = _storage.GetStocks(request.UserId);
                var options = _storage.GetOwnedOptions(request.UserId);

                await Task.WhenAll(stocks, options);

                return Mapper.ToTransactionLog(
                    stocks.Result,
                    options.Result,
                    request.Ticker,
                    request.GroupBy,
                    request.Show,
                    request.TxType);
            }
        }
    }
}