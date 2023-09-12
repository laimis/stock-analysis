using System;
using System.Collections.Generic;
using System.Linq;
using core.Account;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Portfolio;
using core.Stocks;
using core.Stocks.Services.Trading;
using PositionInstance = core.Stocks.PositionInstance;

namespace core.Shared.Adapters.CSV
{
    public class CSVExport
    {
        private const string DATE_FORMAT = "yyyy-MM-dd";
        private record struct PendingPositionRecord(string ticker, decimal bid, decimal numberOfShares, decimal? stopPrice, string date, string closed, bool purchased, string strategy, string notes);
        private record struct StockRecord(string ticker, string type, decimal amount, decimal price, string date, string notes);
        private record struct NoteRecord(string created, string ticker, string note);
        private record struct OptionRecord(string ticker, string type, decimal strike, string optiontype, string expiration, decimal amount, decimal premium, string filled);
        private record struct UserRecord(string email, string first_name, string last_name);
        private record struct CryptosRecord(string symbol, string type, decimal amount, decimal price, string date);
        private record struct TradesRecord(string symbol, string opened, string closed, decimal daysheld, decimal firstbuycost, decimal cost, decimal profit, decimal returnpct, decimal rr, decimal? riskedAmount, string grade, string gradeNote);
        private record struct StockListRecord(string ticker, string created, string notes);
        private record struct StockListRecordJustTicker(string ticker);
        private record struct TradingStrategyResultRecord(string strategyName, string ticker, decimal profit, decimal rr, decimal returnPct, decimal numberOfShares, decimal cost, decimal averageBuyCostPerShare, decimal averageSaleCostPerShare, string opened, string closed, decimal daysHeld);

        public static string Generate(ICSVWriter writer, List<TradingStrategyPerformance> strategies)
        {
            var rows = strategies
                .SelectMany(strategy => 
                    strategy.positions.Select(r =>
                        new TradingStrategyResultRecord(
                            strategy.strategyName,
                            r.Ticker,
                            Math.Round(r.Profit, 2),
                            Math.Round(r.RR, 2),
                            Math.Round(r.GainPct, 2) * 100,
                            r.CompletedPositionShares,
                            r.CompletedPositionShares * r.CompletedPositionCostPerShare,
                            r.CompletedPositionCostPerShare,
                            r.AverageSaleCostPerShare,
                            r.Opened.Value.ToString(DATE_FORMAT),
                            r.Closed.Value.ToString(DATE_FORMAT),
                            r.DaysHeld
                        )
                    )
                );

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, IEnumerable<PendingStockPosition> pending)
        {
            var rows = pending.Select(p => p.State).Select(p => new PendingPositionRecord(
                ticker: p.Ticker,
                bid: p.Bid,
                numberOfShares: p.NumberOfShares,
                stopPrice: p.StopPrice,
                date: p.Date.ToString(DATE_FORMAT),
                closed: p.Closed?.ToString(DATE_FORMAT),
                purchased: p.Purchased,
                strategy: p.Strategy,
                notes: p.Notes
            ));

            return writer.Generate(rows);
        }

        public static string Generate(ICSVWriter writer, StockListState list, bool justTickers)
        {
            return justTickers switch {
                true => writer.Generate(list.Tickers.Select(s => new StockListRecordJustTicker(s.Ticker))),
                false => writer.Generate(list.Tickers.Select(s => new StockListRecord(s.Ticker, s.When.ToString(DATE_FORMAT), s.Note)))
            };
        }

        public static string Generate(ICSVWriter writer, IEnumerable<PositionInstance> trades)
        {
            var rows = trades.Select(t =>
                new TradesRecord(
                    symbol: t.Ticker,
                    opened: t.Opened?.ToString(DATE_FORMAT),
                    closed: t.Closed?.ToString(DATE_FORMAT),
                    daysheld: t.DaysHeld,
                    firstbuycost: t.CompletedPositionCostPerShare,
                    cost: t.Cost,
                    profit: t.Profit,
                    returnpct: t.IsClosed ? t.GainPct : t.UnrealizedGainPct.Value,
                    rr: t.RR,
                    riskedAmount: t.RiskedAmount,
                    grade: t.Grade,
                    gradeNote: t.GradeNote
                )
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

        public static string GenerateFilename(string prefix)
        {
            return prefix + "_" + DateTime.UtcNow.ToString($"yyyyMMdd_hhmss") + ".csv";
        }
    }
}