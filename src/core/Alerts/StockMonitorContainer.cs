using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using core.Stocks;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private ConcurrentDictionary<string, StockPositionMonitor> _monitors = new ConcurrentDictionary<string, StockPositionMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();

        public IEnumerable<StockPositionMonitor> Monitors => _monitors.Values;

        public void Register(OwnedStock stock)
        {
            _tickers.Add(stock.State.Ticker);

            _monitors[ToKey(stock)] = new StockPositionMonitor(stock.State.OpenPosition, stock.State.UserId);
        }

        private static string ToKey(OwnedStock stock) => ToKey(stock.State.Ticker, stock.State.UserId);
        private static string ToKey(string ticker, Guid userId) => userId.ToString() + ticker;

        internal void Deregister(OwnedStock stock) => _monitors.TryRemove(ToKey(stock), out var val);

        public IEnumerable<string> GetTickers() => _tickers;

        public IEnumerable<StockMonitorTrigger> UpdateValue(
            string ticker,
            decimal newPrice,
            DateTimeOffset time)
        {
            foreach (var m in _monitors.Values)
            {
                if (m.CheckTrigger(ticker, newPrice, time))
                {
                    yield return m.Trigger.Value;
                }
            }
        }

        public bool HasTriggered(string ticker, Guid userId)
        {
            _monitors.TryGetValue(ToKey(ticker, userId), out var m);

            return m != null && m.IsTriggered;
        }
    }
}