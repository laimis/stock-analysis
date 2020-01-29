using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using core.Notes;
using core.Options;
using core.Shared;
using core.Stocks;

namespace core
{
    public class CSVExport
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        public const string STOCK_HEADER = "ticker,type,amount,price,date";
        public const string NOTE_HEADER = "created,predicted price,ticker,note";
        public const string OPTION_HEADER = "ticker,strike,type,expiration,filled,amount,premium,closed,spent";

        public static string Generate(IEnumerable<OwnedStock> stocks)
        {
            var rows = stocks.SelectMany(o => o.State.Buys).Cast<AggregateEvent>()
                .Union(stocks.SelectMany(o => o.State.Sells))
                .OrderBy(e => e.When)
                .Select(e => {
                    return StockEventToParts(e);
                });

            return Generate(STOCK_HEADER, rows);
        }

        private static IEnumerable<object> StockEventToParts(object r)
        {
            switch(r)
            {
                case StockPurchased sp:
                    return new object[] {
                        sp.Ticker,
                        "buy",
                        sp.Amount,
                        sp.Price,
                        sp.When.ToString(DATE_FORMAT)
                    };
                
                case StockSold ss:

                    return new object[] {
                        ss.Ticker,
                        "sell",
                        ss.Amount,
                        ss.Price,
                        ss.When.ToString(DATE_FORMAT)
                    };

                default:
                    throw new InvalidOperationException("Invalid stock event for export: " + r.GetType());
            }
        }

        public static string Generate(IEnumerable<Note> notes)
        {
            var rows = notes.Select(n => {
                var state = n.State;

                return new object[]{
                    state.Created.ToString(DATE_FORMAT),
                    state.PredictedPrice,
                    state.RelatedToTicker,
                    "\"" + state.Note.Replace("\"", "\"\"") + "\""
                };
            });

            return Generate(NOTE_HEADER, rows);
        }

        public static string Generate(IEnumerable<SoldOption> options)
        {
            var rows = options.OrderBy(s => s.State.Filled).Select(s => new object[]{
                s.State.Ticker,
                s.State.StrikePrice,
                s.State.Type,
                s.State.Expiration.ToString(DATE_FORMAT),
                s.State.Filled?.ToString(DATE_FORMAT),
                s.State.Amount,
                s.State.Premium,
                s.State.Closed?.ToString(DATE_FORMAT),
                s.State.Spent
            });

            return Generate(OPTION_HEADER, rows);
        }

        private static string Generate(string header, IEnumerable<IEnumerable<object>> rows)
        {
            var builder = new StringBuilder();

            builder.AppendLine(header);
            
            foreach (var r in rows)
            {
                builder.AppendLine(string.Join(",", r));
            }

            return builder.ToString();
        }

        public static string GenerateFilename(string exportType)
        {
            return exportType + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}