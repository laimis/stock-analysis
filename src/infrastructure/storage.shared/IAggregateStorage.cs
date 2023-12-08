using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.fs.Shared.Domain.Accounts;
using core.Shared;

namespace storage.shared
{
    public interface IAggregateStorage
    {
        Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, UserId userId);
        Task SaveEventsAsync(IAggregate agg, string entity, UserId userId, IDbTransaction outsideTransaction = null);
        Task SaveEventsAsync(IAggregate old, IAggregate newAggregate, string entity, UserId userId, IDbTransaction outsideTransaction = null);
        Task DoHealthCheck();
        Task DeleteAggregates(string entity, UserId userId, IDbTransaction outsideTransaction = null);
        Task DeleteAggregate(string entity, Guid aggregateId, UserId userId);
        
        Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, UserId userId);
    }
}