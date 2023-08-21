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
    public static class Monitors
    {
        private const string GAP_UP_TAG = "monitor:gapup";
        public const string PATTERN_TAG = "monitor:patterns";

        public record struct MonitorDescriptor(string tag, string name);
        public static IEnumerable<MonitorDescriptor> GetMonitors()
        {
            yield return new MonitorDescriptor(GAP_UP_TAG,"Gap Up");
            yield return new MonitorDescriptor(PATTERN_TAG, "Patterns");
        }
                
        public static Func<Task<List<AlertCheck>>> GetScannerForTag(
            string tag,
            Func<UserState, string, Task<ServiceResponse<PriceBar[]>>> pricesFunc,
            StockAlertContainer container,
            List<AlertCheck> checks,
            CancellationToken cancellationToken)
        {
            return tag switch {
                PATTERN_TAG => () => MonitorForPatterns(pricesFunc, container, checks, cancellationToken),
                GAP_UP_TAG => () => Task.FromResult(new List<AlertCheck>()),
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
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

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

        private static async Task<List<AlertCheck>> MonitorForPatterns(
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

                foreach(var available in PatternDetection.AvailablePatterns)
                {
                    PatternAlert.Deregister(
                        container: container,
                        ticker: c.ticker,
                        patternName: available,
                        userId: c.user.Id
                    );
                }

                var patterns = PatternDetection.Generate(prices.Success);

                foreach(var pattern in patterns)
                {
                    PatternAlert.Register(
                        container: container,
                        ticker: c.ticker,
                        sourceList: c.listName,
                        pattern: pattern,
                        value: pattern.value,
                        valueFormat: pattern.valueFormat,
                        when: DateTimeOffset.UtcNow,
                        userId: c.user.Id
                    );
                }
            }
        
            return completed;
        }
    }
}