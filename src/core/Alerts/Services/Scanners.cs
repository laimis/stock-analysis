using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;

namespace core.Alerts.Services
{
    public static class Scanners
    {
        private const string GAP_UP_TAG = "monitor:gapup";
        private const string UPSIDE_REVERSAL_TAG = "monitor:upsidereversal";

        public static IEnumerable<string> GetTags()
        {
            yield return GAP_UP_TAG;
            yield return UPSIDE_REVERSAL_TAG;
        }
        
        
        public static Func<Task<List<AlertCheck>>> GetScannerForTag(
            string tag,
            Func<UserState, string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken cancellationToken)
        {
            return tag switch {
                GAP_UP_TAG => () => MonitorForGaps(pricesFunc, container, checks, cancellationToken),
                UPSIDE_REVERSAL_TAG => () => MonitorForUpsideReversals(pricesFunc, container, checks, cancellationToken),
                _ => () => throw new NotImplementedException($"No scanner for tag {tag}")
            };
        }

        public static async Task<List<AlertCheck>> MonitorForStopLosses(
            Func<UserState, string, Task<ServiceResponse<StockQuote>>> quoteFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken cancellationToken)
        {
            var completed = new List<AlertCheck>();

            foreach(var c in checks)
            {
                var priceResponse = await quoteFunc(c.user, c.ticker);
                if (!priceResponse.IsOk)
                {
                    continue;
                }

                var price = priceResponse.Success.lastPrice;
                if (price <= c.threshold.Value)
                {
                    StopPriceMonitor.Register(
                        container: container,
                        price: price,
                        stopPrice: c.threshold.Value,
                        ticker: c.ticker,
                        when: DateTimeOffset.UtcNow,
                        userId: c.user.Id
                    );
                }
                else
                {
                    StopPriceMonitor.Deregister(container, c.ticker, c.user.Id);
                }

                completed.Add(c);
            }

            return completed;
        }

        private static async Task<List<AlertCheck>> MonitorForGaps(
            Func<UserState, string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken ct)
        {
            var completed = new List<AlertCheck>();

            foreach (var c in checks)
            {
                if (ct.IsCancellationRequested)
                {
                    return completed;
                }

                var prices = await pricesFunc(
                    c.user,
                    c.ticker
                );

                if (!prices.IsOk)
                {
                    continue;
                }

                completed.Add(c);

                var gaps = GapAnalysis.Generate(prices.Success, 2);
                if (gaps.Count == 0 || gaps[0].type != GapType.Up)
                {
                    GapUpMonitor.Deregister(container, c.ticker, c.user.Id);
                    continue;
                }

                var gap = gaps[0];

                GapUpMonitor.Register(
                    container: container,
                    ticker: c.ticker, gap: gap, when: DateTimeOffset.UtcNow, userId: c.user.Id
                );
            }
        
            return completed;
        }

        private static async Task<List<AlertCheck>> MonitorForUpsideReversals(
            Func<UserState, string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken ct)
        {
            var completed = new List<AlertCheck>();

            foreach (var c in checks)
            {
                if (ct.IsCancellationRequested)
                {
                    return completed;
                }

                var prices = await pricesFunc(c.user, c.ticker);
                if (!prices.IsOk)
                {
                    continue;
                }

                completed.Add(c);

                foreach(var patternName in PatternDetection.AvailablePatterns)
                {
                    PatternAlert.Deregister(
                        container: container,
                        ticker: c.ticker,
                        patternName: patternName,
                        userId: c.user.Id
                    );
                }

                var patterns = PatternDetection.Generate(prices.Success);

                foreach(var pattern in patterns)
                {
                    PatternAlert.Register(
                        container: container,
                        ticker: c.ticker,
                        pattern: pattern,
                        price: prices.Success[^1].Close,
                        when: DateTimeOffset.UtcNow,
                        userId: c.user.Id
                    );
                }
            }
        
            return completed;
        }
    }
}