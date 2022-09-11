using System;
using System.Collections.Generic;
using System.Linq;
using core.Account;
using core.Alerts;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Stocks;
using core.Stocks.View;

namespace core
{
    public class CSVExport
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        private record struct StockRecord(string ticker, string type, decimal amount, decimal price, string date, string notes);
        private record struct NoteRecord(string created, string ticker, string note);
        private record struct OptionRecord(string ticker, string type, decimal strike, string optiontype, string expiration, decimal amount, decimal premium, string filled);
        private record struct UserRecord(string email, string first_name, string last_name);
        private record struct AlertsRecord(string ticker, string pricepoints);
        private record struct PastTradesRecord(string ticker, string date, decimal profit, decimal percentage);
        private record struct OwnedStocksRecord(string ticker, decimal shares, decimal averagecost, decimal invested, decimal daysheld, string category);
        private record struct CryptosRecord(string symbol, string type, decimal amount, decimal price, string date);
        private record struct TradesRecord(string symbol, string opened, string closed, decimal daysheld, decimal firstbuycost, decimal cost, decimal profit, decimal returnpct, decimal rr, decimal buys, decimal sells);

        public static string Generate(ICSVWriter writer, IEnumerable<Stocks.PositionInstance> trades)
        {
            var rows = trades.Select(t =>
                new TradesRecord(t.Ticker, t.Opened?.ToString(DATE_FORMAT), t.Closed?.ToString(DATE_FORMAT), t.DaysHeld,
                t.FirstBuyCost.Value, t.Cost,
                t.Profit, t.GainPct,t.RR,
                t.NumberOfBuys, t.NumberOfSells)
            );

            return writer.Generate(rows);
        }
        public static string Generate(ICSVWriter writer, IEnumerable<User> users)
        {
            var rows = users.Select(u => new UserRecord(u.State.Email, u.State.Firstname, u.State.Lastname));

            return writer.Generate(rows);
        }
        
        public static string Generate(ICSVWriter writer, IEnumerable<OwnedCrypto> cryptos)
        {
            var rows = cryptos.SelectMany(o => o.State.UndeletedBuysOrSells.Select(e => (o, e)))
                .OrderBy(t => t.e.When)
                .Select(t => t.e switch {
                    CryptoPurchased cp => new CryptosRecord(t.o.State.Token, "buy", cp.Quantity, cp.DollarAmount, cp.When.ToString(DATE_FORMAT)),
                    CryptoSold cs => new CryptosRecord(t.o.State.Token, "sell", cs.Quantity, cs.DollarAmount, cs.When.ToString(DATE_FORMAT)),
                    _ => new CryptosRecord()
                })
                .Where(r => r.symbol != null);

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, IEnumerable<Alert> alerts)
        {
            var rows = alerts.Select(u =>
                new AlertsRecord(u.State.Ticker.Value, string.Join(";", u.State.PricePoints.Select(p => p.Value)))
            );

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, IEnumerable<StockTransactionView> pastTrades)
        {
            var rows = pastTrades.Select(t =>
                new PastTradesRecord(t.Ticker, t.Date, t.Profit, t.ReturnPct)
            );

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, IEnumerable<OwnedStock> stocks)
        {
            var rows = stocks
                .SelectMany(o => o.State.BuyOrSell)
                .Select(e => 
                    e switch {
                        StockPurchased sp => new StockRecord(sp.Ticker, "buy", sp.NumberOfShares, sp.Price, sp.When.ToString(DATE_FORMAT), sp.Notes),
                        StockPurchased_v2 sp => new StockRecord(sp.Ticker, "buy", sp.NumberOfShares, sp.Price, sp.When.ToString(DATE_FORMAT), sp.Notes),
                        StockSold ss => new StockRecord(ss.Ticker, "sell", ss.NumberOfShares, ss.Price, ss.When.ToString(DATE_FORMAT), ss.Notes),
                        _ => new StockRecord()
                    }
                )
                .Where(s => s.ticker != null);

            return writer.Generate(rows);
        }

        private static IEnumerable<object> StockEventToParts((Aggregate a, AggregateEvent ae) tuple)
        {
            switch(tuple.ae)
            {
                case CryptoPurchased cp:

                    return new object[] {
                        cp.Token,
                        "buy",
                        cp.Quantity,
                        cp.DollarAmount,
                        cp.When.ToString(DATE_FORMAT)
                    };

                case CryptoSold cs:

                    return new object[] {
                        cs.Token,
                        "sell",
                        cs.Quantity,
                        cs.DollarAmount,
                        cs.When.ToString(DATE_FORMAT)
                    };

                default:
                    throw new InvalidOperationException("Invalid stock event for export: " + tuple.ae.GetType());
            }
        }

        public static string Generate(ICSVWriter writer, IEnumerable<Note> notes)
        {
            var rows = notes.Select(n => {
                var state = n.State;

                return new NoteRecord(
                    state.Created.ToString(DATE_FORMAT),
                    state.RelatedToTicker,
                    state.Note
                );
            });

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, IEnumerable<OwnedOption> options)
        {
            var rows = options.SelectMany(o => o.State.Buys.Select(op => (o, (AggregateEvent)op)))
                .Union(options.SelectMany(o => o.State.Sells.Select(os => (o, (AggregateEvent)os))))
                .Union(options.SelectMany(o => o.State.Expirations.Select(os => (o, (AggregateEvent)os))))
                .OrderBy(t => t.Item2.When)
                .Select(t => t.Item2 switch {
                    OptionSold s => new OptionRecord(
                        t.o.State.Ticker,
                        "sell",
                        t.o.State.StrikePrice,
                        t.o.State.OptionType.ToString(),
                        t.o.State.Expiration.ToString(DATE_FORMAT),
                        s.NumberOfContracts,
                        s.Premium,
                        s.When.ToString(DATE_FORMAT)
                    ),
                    OptionPurchased p => new OptionRecord(
                        t.o.State.Ticker,
                        "buy",
                        t.o.State.StrikePrice,
                        t.o.State.OptionType.ToString(),
                        t.o.State.Expiration.ToString(DATE_FORMAT),
                        p.NumberOfContracts,
                        p.Premium,
                        p.When.ToString(DATE_FORMAT)
                    ),
                    OptionExpired ex => new OptionRecord(
                        t.o.State.Ticker,
                        ex.Assigned ? "assigned" : "expired",
                        t.o.State.StrikePrice,
                        t.o.State.OptionType.ToString(),
                        t.o.State.Expiration.ToString(DATE_FORMAT),
                        0,
                        0,
                        ex.When.ToString(DATE_FORMAT)
                    ),
                    _ => new OptionRecord()
                })
                .Where(s => s.ticker != null);

            return writer.Generate(rows);
        }

        public static string GenerateFilename(string exportType)
        {
            return exportType + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}