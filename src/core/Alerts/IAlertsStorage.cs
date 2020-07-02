using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Alerts
{
    public interface IAlertsStorage
    {
        Task<Alert> GetAlert(string ticker, Guid userId);
        Task<Alert> GetAlert(Guid alertId, Guid userId);
        Task<IEnumerable<Alert>> GetAlerts(Guid userId);
        Task Save(Alert alert);
        Task Delete(Alert alert);
    }
}