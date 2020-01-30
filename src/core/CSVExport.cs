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
        public const string OPTION_HEADER = "ticker,type,strike,optiontype,expiration,amount,premium,filled";

        public static string Generate(IEnumerable<OwnedStock> stocks)
        {
            var rows = stocks.SelectMany(o => o.State.Buys.Select(op => (o, (AggregateEvent)op)))
                .Union(stocks.SelectMany(o => o.State.Sells.Select(os => (o, (AggregateEvent)os))))
                .OrderBy(t => t.Item2.When)
                .Select(e => {
                    return StockEventToParts(e);
                });

            return Generate(STOCK_HEADER, rows);
        }

        private static IEnumerable<object> StockEventToParts((Aggregate a, AggregateEvent ae) tuple)
        {
            switch(tuple.ae)
            {
                case OptionSold os:
                    var o1 = tuple.a as OwnedOption;
                    return new object[] {
                        o1.State.Ticker,
                        "sell",
                        o1.State.StrikePrice,
                        o1.State.OptionType.ToString(),
                        o1.State.Expiration.ToString(DATE_FORMAT),
                        os.Amount,
                        os.Premium,
                        os.When.ToString(DATE_FORMAT)
                    };

                case OptionPurchased op:
                    var o2 = tuple.a as OwnedOption;
                    return new object[] {
                        o2.State.Ticker,
                        "buy",
                        o2.State.StrikePrice,
                        o2.State.OptionType.ToString(),
                        o2.State.Expiration.ToString(DATE_FORMAT),
                        op.NumberOfContracts,
                        op.Premium,
                        op.When.ToString(DATE_FORMAT)
                    };

                case OptionExpired expired:
                    var o3 = tuple.a as OwnedOption;
                    return new object[] {
                        o3.State.Ticker,
                        "expired",
                        o3.State.StrikePrice,
                        o3.State.OptionType.ToString(),
                        o3.State.Expiration.ToString(DATE_FORMAT),
                        0,
                        0,
                        expired.When.ToString(DATE_FORMAT)
                    };

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
                    throw new InvalidOperationException("Invalid stock event for export: " + tuple.ae.GetType());
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

        public static string Generate(IEnumerable<OwnedOption> options)
        {
            var rows = options.SelectMany(o => o.State.Buys.Select(op => (o, (AggregateEvent)op)))
                .Union(options.SelectMany(o => o.State.Sells.Select(os => (o, (AggregateEvent)os))))
                .Union(options.SelectMany(o => o.State.Expirations.Select(os => (o, (AggregateEvent)os))))
                .OrderBy(t => t.Item2.When)
                .Select(e => {
                    return StockEventToParts(e);
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