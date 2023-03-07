using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Analysis;

namespace core.Alerts.Services
{
    public static class Monitors
    {
        private const string GAP_UP_TAG = "monitor:gapup";
        private const string UPSIDE_REVERSAL_TAG = "monitor:upsidereversal";
        private const string UNUSUAL_VOLUME_TAG = "monitor:unusualvolume";

        public record struct MonitorDescriptor(string tag, string name);
        public static IEnumerable<MonitorDescriptor> GetMonitors()
        {
            yield return new MonitorDescriptor(GAP_UP_TAG,"Gap Up");
            yield return new MonitorDescriptor(UPSIDE_REVERSAL_TAG, "Upside Reversal");
            yield return new MonitorDescriptor(UNUSUAL_VOLUME_TAG, "Unusual Volume");
        }
        
        
        public static Func<Task<List<AlertCheck>>> GetScannerForTag(
            string tag,
            Func<string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken cancellationToken)
        {
            return tag switch {
                GAP_UP_TAG => () => MonitorForGaps(pricesFunc, container, checks, cancellationToken),
                UPSIDE_REVERSAL_TAG => () => MonitorForUpsideReversals(pricesFunc, container, checks, cancellationToken),
                UNUSUAL_VOLUME_TAG => () => MonitorForUnusualVolume(pricesFunc, container, checks, cancellationToken),
                _ => () => throw new NotImplementedException($"No scanner for tag {tag}")
            };
        }

        private static async Task<List<AlertCheck>> MonitorForUnusualVolume(Func<string, Task<ServiceResponse<PriceBar[]>>> pricesFunc, StockAlertContainer container, List<AlertCheck> checks, CancellationToken cancellationToken)
        {
            var completed = new List<AlertCheck>();

            foreach (var c in checks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return completed;
                }

                var prices = await pricesFunc(c.ticker);
                if (!prices.IsOk)
                {
                    continue;
                }

                completed.Add(c);

                var detectedPatterns = PatternDetection.Generate(prices.Success);
                var volumePatterns = detectedPatterns.Where(
                    p => p.name == PatternDetection.Highest1YearVolumeName ||
                         p.name == PatternDetection.HighVolumeName
                );

                if (volumePatterns.Any())
                {
                    var patternsFound = string.Join(",", volumePatterns.Select(p => p.name).ToArray());

                    UnusualVolumeMonitor.Register(
                        container: container,
                        ticker: c.ticker,
                        patternsFound: patternsFound,
                        volume: prices.Success[^1].Volume,
                        when: DateTimeOffset.UtcNow,
                        userId: c.user.Id
                    );
                }
                else
                {
                    UnusualVolumeMonitor.Deregister(container, c.ticker, c.user.Id);
                }
            }

            return completed;
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

                var price = priceResponse.Success.Price;
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
            Func<string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
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

                var prices = await pricesFunc(c.ticker);

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
            Func<string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
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

                var prices = await pricesFunc(c.ticker);
                if (!prices.IsOk)
                {
                    continue;
                }

                completed.Add(c);

                PatternAlert.Deregister(
                    container: container,
                    ticker: c.ticker,
                    patternName: PatternDetection.UpsideReversalName,
                    userId: c.user.Id
                );

                var upsideReversal = PatternDetection
                    .Generate(prices.Success)
                    .Where(p => p.name == PatternDetection.UpsideReversalName)
                    .SingleOrDefault();

                if (upsideReversal != null)
                {
                    PatternAlert.Register(
                        container: container,
                        ticker: c.ticker,
                        pattern: upsideReversal,
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