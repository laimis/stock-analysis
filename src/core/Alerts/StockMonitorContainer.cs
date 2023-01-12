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
        private const int MAX_RECENT_ALERTS = 20;
        private const int RECENT_ALERT_HOUR_THRESHOLD = 8; // alerts within eight hour are considered to be recent

        public IEnumerable<IStockPositionMonitor> Monitors => _monitors.Values;

        public void Register(OwnedStockState stock)
        {   
            void AddIfNotNull(IStockPositionMonitor monitor)
            {
                if (monitor != null)
                {
                    Register(monitor);
                }
            }
            
            var stopMonitor = StopPriceMonitor.CreateIfApplicable(stock);
            AddIfNotNull(stopMonitor);

            var profitMonitor = ProfitPriceMonitor.CreateIfApplicable(stock);
            AddIfNotNull(profitMonitor);
        }

        public void Register(IStockPositionMonitor monitor)
        {
            _monitors[new StockPositionMonitorKey(monitor.Description, monitor.Ticker, monitor.UserId)] = monitor;
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

        public void Deregister(string identifier, string ticker, Guid userId)
        {
            var key = new StockPositionMonitorKey(identifier, ticker, userId);
            _monitors.TryRemove(key, out _);
        }

        public void AddToRecent(TriggeredAlert triggeredAlert)
        {
            if (!_recentlyTriggeredAlerts.ContainsKey(triggeredAlert.userId))
            {
                _recentlyTriggeredAlerts[triggeredAlert.userId] = new List<TriggeredAlert>(MAX_RECENT_ALERTS);
            }

            var list = _recentlyTriggeredAlerts[triggeredAlert.userId];

            if (list.Count + 1 == MAX_RECENT_ALERTS)
            {
                list.RemoveAt(0);
            }

            list.Add(triggeredAlert);
        }

        public bool HasRecentlyTriggered(TriggeredAlert a)
        {
            if (_recentlyTriggeredAlerts.TryGetValue(a.userId, out var triggeredAlerts))
            {
                return triggeredAlerts.Any(
                    triggeredAlert => 
                        triggeredAlert.MatchesTickerAndSource(a)
                        && triggeredAlert.AgeInHours < RECENT_ALERT_HOUR_THRESHOLD
                    );
            }

            return false;
        }
    }
}