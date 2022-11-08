using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using core.Stocks;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private record struct StockPositionMonitorKey(string MonitorIdentifier, string Ticker, Guid UserId);
        private ConcurrentDictionary<StockPositionMonitorKey, IStockPositionMonitor> _monitors = new ConcurrentDictionary<StockPositionMonitorKey, IStockPositionMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();
        private const int MAX_RECENT_ALERTS = 20;

        public IEnumerable<IStockPositionMonitor> Monitors => _monitors.Values;

        public void Register(OwnedStock stock)
        {
            
            void AddIfNotNull(IStockPositionMonitor monitor)
            {
                if (monitor != null)
                {
                    _monitors[new StockPositionMonitorKey(monitor.MonitorIdentifer, monitor.Ticker, monitor.UserId)] = monitor;
                }
            }

            _tickers.Add(stock.State.Ticker);
            
            var stopMonitor = StopPriceMonitor.CreateIfApplicable(stock.State);
            AddIfNotNull(stopMonitor);

            var profitMonitorR1 = ProfitPriceMonitor.CreateIfApplicable(stock.State, 0);
            AddIfNotNull(profitMonitorR1);

            var profitMonitorR2 = ProfitPriceMonitor.CreateIfApplicable(stock.State, 1);
            AddIfNotNull(profitMonitorR2);

            var profitMonitorR3 = ProfitPriceMonitor.CreateIfApplicable(stock.State, 2);
            AddIfNotNull(profitMonitorR3);
        }

        private Dictionary<Guid, List<TriggeredAlert>> _recentlyTriggeredAlerts = new Dictionary<Guid, List<TriggeredAlert>>();

        internal List<TriggeredAlert> GetRecentlyTriggeredAlerts(Guid userId) =>
            (_recentlyTriggeredAlerts.SingleOrDefault(u => u.Key == userId).Value ?? new List<TriggeredAlert>())
            .OrderByDescending(a => a.alertType)
            .ThenByDescending(a => a.when)
            .ToList();

        internal List<IStockPositionMonitor> GetMonitors(Guid userId) => 
            _monitors.Values.Where(m => m.UserId == userId).ToList();

        private static string ToKey(string ticker, Guid userId) => userId.ToString() + ticker;

        internal void Deregister(OwnedStock stock)
        {
            var keysToRemove = _monitors.Keys.Where(k => k.Ticker == stock.State.Ticker && k.UserId == stock.State.UserId).ToList();
            
            foreach(var key in keysToRemove)
            {
                _monitors.TryRemove(key, out _);
            }
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

        public bool HasRecentlyTriggered(TriggeredAlert a)
        {
            if (_recentlyTriggeredAlerts.TryGetValue(a.userId, out var list))
            {
                return list.Any(r => r.source == a.source && r.ticker == a.ticker && a.id != r.id);
            }

            return false;
        }
    }
}