using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared;

namespace storage.shared
{
    public interface IAggregateStorage
    {
        Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string key, string userId);
        Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string userId);
        Task SaveEventsAsync(Aggregate agg, string entity);
        Task DoHealthCheck();
    }
}