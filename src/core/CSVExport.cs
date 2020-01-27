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
        public static string Generate(IEnumerable<OwnedStock> stocks)
        {
            var rows = stocks.SelectMany(o => o.State.Buys).Cast<AggregateEvent>()
                .Union(stocks.SelectMany(o => o.State.Sells))
                .OrderBy(e => e.When);

            return Generate(
                GetStocksExportHeaders(),
                rows,
                r => StockEventToParts(r));
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
                        sp.When.ToString("yyyy-MM-dd")
                    };
                
                case StockSold ss:

                    return new object[] {
                        ss.Ticker,
                        "sell",
                        ss.Amount,
                        ss.Price,
                        ss.When.ToString("yyyy-MM-dd")
                    };

                default:
                    throw new InvalidOperationException("Invalid stock event for export: " + r.GetType());
            }
        }

        public static string Generate(IEnumerable<Note> notes)
        {
            return Generate(
                GetNotesExportHeaders(),
                notes,
                o => GetExportParts((o as Note).State));
        }

        public static string Generate(IEnumerable<SoldOption> options)
        {
            var rows = options.Select(s => new object[]{
                s.State.Ticker,
                s.State.StrikePrice,
                s.State.Type,
                s.State.Expiration.ToString("yyyy-MM-dd"),
                s.State.Filled?.ToString("yyyy-MM-dd"),
                s.State.Amount,
                s.State.Premium,
                s.State.Closed?.ToString("yyyy-MM-dd"),
                s.State.Spent,
                s.State.Profit
            });

            return Generate(
                GetOptionsExportHeaders(),
                rows,
                o => (object[])o);
        }

        public static string GetOptionsExportHeaders()
        {
            return "ticker,strike price,type,expiration,filled,amount,premium,closed,spent,profit";
        }

        private static string Generate(
            string header,
            IEnumerable<object> objs,
            Func<object, IEnumerable<object>> rowGenerator)
        {
            var builder = new StringBuilder();

            builder.AppendLine(header);
            
            foreach (var s in objs)
            {
                builder.AppendLine(string.Join(",", rowGenerator(s)));
            }

            return builder.ToString();
        }

        private static object[] GetExportParts(SoldOptionState state)
        {
            return new object[]{
                state.Ticker,
                state.StrikePrice,
                state.Type.ToString(),
                state.Expiration.ToString("yyyy-MM-dd"),
                state.Closed != null ? state.Closed.Value.ToString("yyyy-MM-dd") : null,
                state.Amount,
                state.Premium,
                state.Spent,
                state.Profit
            };
        }

        public static string GetStocksExportHeaders()
        {
            return "ticker,type,amount,price,date";
        }

        private static object[] GetExportParts(NoteState state)
        {
            return new object[]{
                state.Created.ToString("yyyy-MM-dd"),
                state.PredictedPrice,
                state.RelatedToTicker,
                "\"" + state.Note.Replace("\"", "\"\"") + "\""
            };
        }

        public static string GetNotesExportHeaders()
        {
            return "created,predicted price,ticker,note";
        }

        public static string GenerateFilename(string exportType)
        {
            return exportType + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}