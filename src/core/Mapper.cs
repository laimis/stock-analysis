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
            string show)
        {
            var log = new List<Shared.Transaction>();
            var tickers = stocks.Select(s => s.Ticker).Union(options.Select(o => o.Ticker))
                .Distinct()
                .OrderBy(s => s);

            if (string.IsNullOrEmpty(show) || show == "shares")
            {
                log.AddRange(
                    stocks.Where(s => s.State.Ticker == (ticker != null ? ticker : s.State.Ticker))
                        .SelectMany(s => s.State.Transactions)
                );
            }

            if (string.IsNullOrEmpty(show) || show == "options")
            {
                log.AddRange(
                    options.Where(o => o.State.Ticker == (ticker != null ? ticker : o.State.Ticker))
                        .SelectMany(o => o.State.Transactions)
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
            StockAdvancedStats stats,
            HistoricalResponse data,
            MetricsResponse metrics)
        {
            var byMonth = data?.Historical?.GroupBy(r => r.Date.ToString("yyyy-MM-01"))
                .Select(g => new
                {
                    Date = DateTime.Parse(g.Key),
                    Price = g.Average(p => p.Close),
                    Volume = Math.Round(g.Average(p => p.Volume) / 1000000.0, 2),
                    Low = g.Min(p => p.Close),
                    High = g.Max(p => p.Close)
                });

            var labels = byMonth?.Select(a => a.Date.ToString("MMMM"));
            var lowValues = byMonth?.Select(a => Math.Round(a.Low, 2));
            var highValues = byMonth?.Select(a => Math.Round(a.High, 2));

            var priceValues = byMonth?.Select(a => Math.Round(a.Price, 2));
            var priceChartData = labels?.Zip(priceValues, (l, p) => new object[] { l, p });

            var volumeValues = byMonth?.Select(a => a.Volume);
            var volumeChartData = labels?.Zip(volumeValues, (l, p) => new object[] { l, p });

            var metricDates = metrics?.Metrics?.Select(m => m.Date.ToString("MM/yy")).Reverse();

            var bookValues = metrics?.Metrics?.Select(m => m.BookValuePerShare).Reverse();
            var bookChartData = metricDates?.Zip(bookValues, (l, p) => new object[] { l, p });

            var peValues = metrics?.Metrics?.Select(m => m.PERatio).Reverse();
            var peChartData = metricDates?.Zip(peValues, (l, p) => new object[] { l, p });

            return new
            {
                ticker,
                price,
                stats,
                profile,
                labels,
                priceChartData,
                volumeChartData,
                bookChartData,
                peChartData
            };
        }

        public static object ToOptionView(OwnedOption o, TickerPrice currentPrice)
        {
            return new
            {
                id = o.State.Id,
                ticker = o.State.Ticker,
                currentPrice = currentPrice.Amount,
                optionType = o.State.OptionType.ToString(),
                strikePrice = o.State.StrikePrice,
                premium = o.State.Credit - o.State.Debit,
                expirationDate = o.State.Expiration.ToString("yyyy-MM-dd"),
                numberOfContracts = Math.Abs(o.State.NumberOfContracts),
                boughtOrSold = o.State.NumberOfContracts > 0 ? "Bought" : "Sold",
                transactions = new TransactionList(o.State.Transactions, null, null),
                expiresSoon = o.ExpiresSoon,
                isExpired = o.IsExpired
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