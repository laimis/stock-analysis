using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using core.Stocks;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private ConcurrentDictionary<string, IStockPositionMonitor> _monitors = new ConcurrentDictionary<string, IStockPositionMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();

        public IEnumerable<IStockPositionMonitor> Monitors => _monitors.Values;

        public void Register(OwnedStock stock)
        {
            
            void AddIfNotNull(string key, IStockPositionMonitor monitor)
            {
                if (monitor != null)
                {
                    _monitors[key + monitor.GetType().Name] = monitor;
                }
            }

            _tickers.Add(stock.State.Ticker);
            
            var key = ToKey(stock);
            var stopMonitor = StopPriceMonitor.CreateIfApplicable(stock.State);
            AddIfNotNull(key, stopMonitor);

            var profitMonitor = ProfitPriceMonitor.CreateIfApplicable(stock.State);
            AddIfNotNull(key, profitMonitor);
        }

        private static string ToKey(OwnedStock stock) => ToKey(stock.State.Ticker, stock.State.UserId);
        private static string ToKey(string ticker, Guid userId) => userId.ToString() + ticker;

        internal void Deregister(OwnedStock stock)
        {
            var key = ToKey(stock);
            _monitors.TryRemove(key + nameof(StopPriceMonitor), out _);
            _monitors.TryRemove(key + nameof(ProfitPriceMonitor), out _);
        }

        public IEnumerable<string> GetTickers() => _tickers;

        public IEnumerable<TriggeredAlert> RunCheck(
            string ticker,
            decimal newPrice,
            DateTimeOffset time)
        {
            foreach (var m in _monitors.Values)
            {
                if (m.RunCheck(ticker, newPrice, time))
                {
                    yield return m.TriggeredAlert.Value;
                }
            }
        }

        public bool HasTriggered(string ticker, Guid userId)
        {
            return _monitors.Values.Where(m => m.Ticker == ticker && m.UserId == userId)
                .Where(m => m.IsTriggered)
                .Any();
        }
    }
}