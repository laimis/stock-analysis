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
        
        private bool _manualRun = false;
        public void RequestManualRun() => _manualRun = true;
        public bool ManualRunRequested() => _manualRun;
        public void ManualRunCompleted() => _manualRun = false;

        public void Register(TriggeredAlert alert)
        {
            var key = new StockPositionMonitorKey(alert.identifier, alert.ticker, alert.userId);

            var firstAdd = !_alerts.ContainsKey(key);

            _alerts[key] = alert;

            if (firstAdd)
            {
                AddToRecent(alert);
            }
        }

        public void Deregister(string identifier, string ticker, Guid userId) =>
            _alerts.TryRemove(
                new StockPositionMonitorKey(identifier, ticker, userId),
                out _
            );
            

        private Dictionary<Guid, List<TriggeredAlert>> _recentlyTriggeredAlerts =
            new Dictionary<Guid, List<TriggeredAlert>>();

        public List<TriggeredAlert> GetRecentlyTriggeredAlerts(Guid userId) =>
            (_recentlyTriggeredAlerts.SingleOrDefault(u => u.Key == userId).Value ?? new List<TriggeredAlert>())
            .OrderByDescending(a => a.when)
            .ToList();

        public List<TriggeredAlert> GetAlerts(Guid userId) => 
            _alerts.Values.Where(m => m.userId == userId)
            .ToList();

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

        public record struct AlertContainerMessage(string message, DateTimeOffset when);
        private int _messageLimit = 10;
        private List<AlertContainerMessage> _messages = new List<AlertContainerMessage>();
        public void AddNotice(string message)
        {
            _messages.Add(new AlertContainerMessage(message, DateTimeOffset.Now));
            if (_messages.Count > _messageLimit)
            {
                _messages.RemoveAt(0);
            }
        }

        public IEnumerable<AlertContainerMessage> GetMessages() =>
            _messages.OrderByDescending(m => m.when);

        private bool _listChecksCompleted;
        private bool _stopLossCheckCompleted;

        public void SetListCheckCompleted(bool completed) => _listChecksCompleted = completed;
        public void SetStopLossCheckCompleted(bool completed) => _stopLossCheckCompleted = completed;
        public bool ContainerReadyForNotifications() => _listChecksCompleted && _stopLossCheckCompleted;
    }
}