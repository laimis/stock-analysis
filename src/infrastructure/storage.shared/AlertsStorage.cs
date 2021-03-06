using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Alerts;

namespace storage.shared
{
    public class AlertsStorage : IAlertsStorage
    {
        public const string _alert_entity = "alerts";
        
        private IAggregateStorage _aggregateStorage;

        public AlertsStorage(IAggregateStorage aggregateStorage)
        {
            _aggregateStorage = aggregateStorage;
        }

        public async Task<IEnumerable<Alert>> GetAlerts(Guid userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_alert_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new Alert(g));
        }

        public async Task<Alert> GetAlert(string ticker, Guid userId)
        {
            var alerts = await GetAlerts(userId);
            
            return alerts.SingleOrDefault(s => s.State.Ticker == ticker);
        }

        public async Task<Alert> GetAlert(Guid alertId, Guid userId)
        {
            var alerts = await GetAlerts(userId);
            
            return alerts.SingleOrDefault(s => s.Id == alertId);
        }

        public Task Save(Alert alert)
        {
            return _aggregateStorage.SaveEventsAsync(alert, _alert_entity, alert.State.UserId);
        }

        public Task Delete(Guid userId)
        {
            return _aggregateStorage.DeleteAggregates(_alert_entity, userId);
        }
    }
}