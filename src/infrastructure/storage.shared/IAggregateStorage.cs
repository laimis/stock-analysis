using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Shared;

namespace storage.shared
{
    public interface IAggregateStorage
    {
        Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid userId);
        Task SaveEventsAsync(Aggregate agg, string entity, Guid userId);
        Task DoHealthCheck();
        Task DeleteAggregates(string entity, Guid userId);
        Task DeleteAggregate(string entity, Guid aggregateId, Guid userId);
        
        Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, Guid userId);
    }
}