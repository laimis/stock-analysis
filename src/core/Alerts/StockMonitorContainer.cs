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
        private const int MAX_RECENT_ALERTS = 20;

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

        private Dictionary<Guid, List<TriggeredAlert>> _recentlyTriggeredAlerts = new Dictionary<Guid, List<TriggeredAlert>>();

        internal List<TriggeredAlert> GetRecentlyTriggeredAlerts(Guid userId) =>
            (_recentlyTriggeredAlerts.SingleOrDefault(u => u.Key == userId).Value ?? new List<TriggeredAlert>())
            .OrderByDescending(a => a.alertType)
            .ThenByDescending(a => a.when)
            .ToList();

        internal List<IStockPositionMonitor> GetMonitors(Guid userId) => 
            _monitors.Values.Where(m => m.UserId == userId).ToList();

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
                    AddToRecent(m.TriggeredAlert);
                    
                    yield return m.TriggeredAlert.Value;
                }
            }
        }

        private void AddToRecent(TriggeredAlert? triggeredAlert)
        {
            if (!_recentlyTriggeredAlerts.ContainsKey(triggeredAlert.Value.userId))
            {
                _recentlyTriggeredAlerts[triggeredAlert.Value.userId] = new List<TriggeredAlert>(MAX_RECENT_ALERTS);
            }

            var list = _recentlyTriggeredAlerts[triggeredAlert.Value.userId];

            if (list.Count + 1 == MAX_RECENT_ALERTS)
            {
                list.RemoveAt(0);
            }

            list.Add(triggeredAlert.Value);
        }

        public bool HasTriggered(string ticker, Guid userId)
        {
            return _monitors.Values.Where(m => m.Ticker == ticker && m.UserId == userId)
                .Where(m => m.IsTriggered)
                .Any();
        }

        public bool HasRecentlyTriggered(TriggeredAlert a)
        {
            if (_recentlyTriggeredAlerts.TryGetValue(a.userId, out var list))
            {
                return list.Any(r => r.source == a.source && r.ticker == a.ticker);
            }

            return false;
        }
    }
}