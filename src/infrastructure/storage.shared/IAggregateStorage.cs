using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared;

namespace storage.shared
{
    public interface IAggregateStorage
    {
        Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string userId);
        Task SaveEventsAsync(Aggregate agg, string entity, string userId);
        Task DoHealthCheck();
        Task DeleteEvents(string entity, string userId);
        
        Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, string userId);
    }
}