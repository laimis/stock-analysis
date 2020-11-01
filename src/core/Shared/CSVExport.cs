using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using core.Account;
using core.Alerts;
using core.Notes;
using core.Options;
using core.Shared;
using core.Stocks;
using core.Stocks.View;

namespace core
{
    public class CSVExport
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        public const string STOCK_HEADER = "ticker,type,amount,price,date";
        public const string NOTE_HEADER = "created,ticker,note";
        public const string OPTION_HEADER = "ticker,type,strike,optiontype,expiration,amount,premium,filled";
        public const string USER_HEADER = "email,first_name,last_name";
        public const string ALERTS_HEADER = "ticker,pricepoints";
        public const string PAST_TRADES_HEADER = "ticker,date,profit,percentage";
        public const string OWNED_STOCKS_HEADER = "ticker,shares,averagecost,invested,daysheld,category";

        public static string Generate(IEnumerable<User> users)
        {
            var rows = users.Select(u =>
                new object[] { u.State.Email, u.State.Firstname, u.State.Lastname}
            );

            return Generate(USER_HEADER, rows);
        }

        public static string Generate(IEnumerable<OwnedStockView> stocks)
        {
            var rows = stocks.Select(s =>
                new object[] { s.Ticker, s.Owned, s.AverageCost, s.Cost, s.DaysHeld, s.Category}
            );

            return Generate(OWNED_STOCKS_HEADER, rows);
        }

        public static string Generate(IEnumerable<Alert> alerts)
        {
            var rows = alerts.Select(u =>
                new object[] { u.State.Ticker.Value, string.Join(";", u.State.PricePoints.Select(p => p.Value))}
            );

            return Generate(ALERTS_HEADER, rows);
        }

        public static string Generate(IEnumerable<StockTransactionView> pastTrades)
        {
            var rows = pastTrades.Select(t =>
                new object[] { t.Ticker, t.Date, t.Profit, t.ReturnPct }
            );

            return Generate(PAST_TRADES_HEADER, rows);
        }

        public static string Generate(IEnumerable<OwnedStock> stocks)
        {
            var rows = stocks.SelectMany(o => o.State.BuyOrSell.Select(op => (o, (AggregateEvent)op)))
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
                        os.NumberOfContracts,
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
                        op.Premium * -1,
                        op.When.ToString(DATE_FORMAT)
                    };

                case OptionExpired expired:
                    var o3 = tuple.a as OwnedOption;
                    return new object[] {
                        o3.State.Ticker,
                        expired.Assigned ? "assigned" : "expired",
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
                        sp.NumberOfShares,
                        sp.Price,
                        sp.When.ToString(DATE_FORMAT)
                    };
                
                case StockSold ss:

                    return new object[] {
                        ss.Ticker,
                        "sell",
                        ss.NumberOfShares,
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