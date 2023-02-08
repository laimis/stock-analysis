using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace core.Alerts
{
    public class StockAlertContainer
    {
        private record struct StockPositionMonitorKey(string description, string ticker, Guid userId);
        private ConcurrentDictionary<StockPositionMonitorKey, TriggeredAlert> _alerts =
            new ConcurrentDictionary<StockPositionMonitorKey, TriggeredAlert>();
        private const int MAX_RECENT_ALERTS = 20;
        private const int RECENT_ALERT_HOUR_THRESHOLD = 8; // alerts within eight hour are considered to be recent

        public IEnumerable<TriggeredAlert> Alerts => _alerts.Values;

        public void Register(TriggeredAlert alert)
        {
            _alerts[new StockPositionMonitorKey(alert.description, alert.ticker, alert.userId)] = alert;

            AddToRecent(alert);
        }
            

        private Dictionary<Guid, List<TriggeredAlert>> _recentlyTriggeredAlerts =
            new Dictionary<Guid, List<TriggeredAlert>>();

        internal List<TriggeredAlert> GetRecentlyTriggeredAlerts(Guid userId) =>
            (_recentlyTriggeredAlerts.SingleOrDefault(u => u.Key == userId).Value ?? new List<TriggeredAlert>())
            .OrderByDescending(a => a.alertType)
            .ThenByDescending(a => a.when)
            .ToList();

        internal List<TriggeredAlert> GetAlerts(Guid userId) => 
            _alerts.Values.Where(m => m.userId == userId)
            .ToList();

        public void Deregister(string description, string ticker, Guid userId) =>
            _alerts.TryRemove(
                new StockPositionMonitorKey(description, ticker, userId),
                out _
            );

        private void AddToRecent(TriggeredAlert triggeredAlert)
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
    }
}