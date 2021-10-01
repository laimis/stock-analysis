using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Cryptos;
using core.Options;
using core.Portfolio.Output;
using core.Shared;
using core.Stocks;

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
                var crypto = _storage.GetCryptos(request.UserId);

                await Task.WhenAll(stocks, options, crypto);

                return ToTransactionLog(
                    stocks.Result,
                    options.Result,
                    crypto.Result,
                    request.Ticker,
                    request.GroupBy,
                    request.Show,
                    request.TxType);
            }

            internal static TransactionList ToTransactionLog(
                IEnumerable<OwnedStock> stocks,
                IEnumerable<OwnedOption> options,
                IEnumerable<OwnedCrypto> cryptos,
                string ticker,
                string groupBy,
                string show,
                string txType)
            {
                var log = new List<Shared.Transaction>();
                var tickers = stocks.Select(s => s.State.Ticker).Union(options.Select(o => o.State.Ticker))
                    .Distinct()
                    .OrderBy(s => s);

                if (string.IsNullOrEmpty(show) || show == "shares")
                {
                    log.AddRange(
                        stocks.Where(s => s.State.Ticker == (ticker != null ? ticker : s.State.Ticker))
                            .SelectMany(s => s.State.Transactions)
                            .Where(t => txType == "pl" ? t.IsPL : !t.IsPL)
                    );
                }

                if (string.IsNullOrEmpty(show) || show == "options")
                {
                    log.AddRange(
                        options.Where(o => o.State.Ticker == (ticker != null ? ticker : o.State.Ticker))
                            .SelectMany(o => o.State.Transactions)
                            .Where(t => txType == "pl" ? t.IsPL : !t.IsPL)
                    );
                }

                if (show == "crypto")
                {
                    log.AddRange(
                        cryptos.Where(o => o.State.Token == (ticker != null ? ticker : o.State.Token))
                            .SelectMany(o => o.State.Transactions.Select(t => t.ToSharedTransaction()))
                            .Where(t => txType == "pl" ? t.IsPL : !t.IsPL)
                    );
                }

                return new TransactionList(log, groupBy, tickers);
            }

        }
    }
}