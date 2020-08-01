using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Options;
using core.Adapters.Stocks;
using core.Notes;
using core.Notes.Output;
using core.Options;
using core.Portfolio.Output;
using core.Stocks;

namespace core
{
    public class Mapper
    {
        public static OptionDetailsViewModel MapOptionDetails(
            double price,
            IEnumerable<OptionDetail> options)
        {
            var optionList = options
                .Where(o => o.Volume > 0 || o.OpenInterest > 0)
                .Where(o => o.ParsedExpirationDate > DateTime.UtcNow)
                .OrderBy(o => o.ExpirationDate)
                .ToArray();

            var expirations = optionList.Select(o => o.ExpirationDate)
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            var puts = optionList.Where(o => o.IsPut);
            var calls = optionList.Where(o => o.IsCall);

            return new OptionDetailsViewModel
            {
                StockPrice = price,
                Options = optionList,
                Expirations = expirations,
                LastUpdated = optionList.Max(o => o.LastUpdated),
                Breakdown = new OptionBreakdownViewModel
                {
                    CallVolume = calls.Sum(o => o.Volume),
                    CallSpend = calls.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnCalls = calls.Average(o => o.Volume * o.StrikePrice) / calls.Average(o => o.Volume),

                    PutVolume = puts.Sum(o => o.Volume),
                    PutSpend = puts.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnPuts = puts.Average(o => o.Volume * o.StrikePrice) / puts.Average(o => o.Volume),
                }
            };
        }

        internal static TransactionList ToTransactionLog(
            IEnumerable<OwnedStock> stocks,
            IEnumerable<OwnedOption> options,
            string ticker,
            string groupBy,
            string show,
            string txType)
        {
            var log = new List<Shared.Transaction>();
            var tickers = stocks.Select(s => s.Ticker).Union(options.Select(o => o.State.Ticker))
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

            return new TransactionList(log, groupBy, tickers);
        }

        internal static NotesList MapNotes(IEnumerable<Note> notes)
        {
            return new NotesList (notes.Select(n => n.State));
        }

        public static object MapLists(List<StockQueryResult> active, List<StockQueryResult> gainers, List<StockQueryResult> losers)
        {
            return new {
                active,
                gainers,
                losers
            };
        }

        public static object MapStockDetail(
            string ticker,
            double price,
            CompanyProfile profile,
            StockAdvancedStats stats)
        {
            return new
            {
                ticker,
                price,
                stats,
                profile,
            };
        }

        internal static object ToOwnedView(OwnedStock o, TickerPrice price)
        {
            var equity = o.State.Owned * price.Amount;
            var cost = o.State.Cost;
            var profits = equity - cost;
            var profitsPct = profits / (1.0 * cost);

            return new
            {
                id = o.Id,
                currentPrice = price.Amount,
                ticker = o.State.Ticker,
                owned = o.State.Owned,
                equity = equity,
                description = o.State.Description,
                averageCost = o.State.AverageCost,
                cost = cost,
                profits = profits,
                profitsPct = profitsPct,
                transactions = new TransactionList(o.State.Transactions, null, null)
            };
        }
    }
}