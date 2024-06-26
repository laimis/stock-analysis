using System.Data;
using core.fs.Accounts;
using core.Shared;
using Microsoft.FSharp.Core;
using storage.shared;

namespace storage.memory;

public class MemoryAggregateStorage(IOutbox outbox) : IAggregateStorage, IBlobStorage
{
    private static readonly Dictionary<string, List<StoredAggregateEvent>> _aggregates = new();
    
    private static readonly Dictionary<string, object> _blobs = new();

    public Task DeleteAggregates(string entity, UserId userId, IDbTransaction? outsideTransaction = null)
    {
        _aggregates.Remove(MakeKey(entity, userId));
        return Task.CompletedTask;
    }

    public Task DeleteAggregate(string entity, Guid aggregateId, UserId userId)
    {
        var key = MakeKey(entity, userId);

        var listOfEvents = _aggregates[key];

        var eventsToRemove = listOfEvents.Where(e => e.Event.AggregateId == aggregateId).ToList();

        foreach (var e in eventsToRemove)
        {
            listOfEvents.Remove(e);
        }

        return Task.CompletedTask;
    }

    private string MakeKey(string entity, UserId aggregateId) => $"{entity}-{aggregateId}";

    public Task DoHealthCheck() => Task.CompletedTask;

    public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, UserId userId)
    {
        var events = await GetStoredEvents(entity, userId);

        return events.Select(e => e.Event);
    }
    
    public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid aggregateId, UserId userId)
    {
        var events = await GetStoredEvents(entity, userId);

        return events.Where(e => e.Event.AggregateId == aggregateId).Select(e => e.Event);
    }

    public Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, UserId userId)
    {
        var key = MakeKey(entity, userId);
        if (!_aggregates.ContainsKey(key))
        {
            return Task.FromResult(Enumerable.Empty<StoredAggregateEvent>());
        }
        return Task.FromResult(_aggregates[key].AsEnumerable());
    }
    
    private async Task SaveEventsInternal(IAggregate agg, int fromVersion, string entity, UserId userId, IDbTransaction? _ = null)
    {
        var key = MakeKey(entity, userId);

        if (!_aggregates.ContainsKey(key))
        {
            _aggregates[key] = new List<StoredAggregateEvent>();
        }

        var version = fromVersion;

        var eventsToBlast = new List<AggregateEvent>();

        foreach (var e in agg.Events.Skip(fromVersion))
        {
            var se = new StoredAggregateEvent
            {
                Entity = entity,
                Event = e,
                Key = e.Id.ToString(),
                UserId = userId.Item,
                Created = e.When,
                Version = ++version
            };

            _aggregates[key].Add(se);
            eventsToBlast.Add(e);
        }

        await outbox.AddEvents(eventsToBlast, null);
    }

    public Task SaveEventsAsync(IAggregate agg, string entity, UserId userId, IDbTransaction? outsideTransaction = null)
    {
        return SaveEventsInternal(agg, agg.Version, entity, userId, outsideTransaction);
    }
    
    public Task SaveEventsAsync(IAggregate? oldAgg, IAggregate newAgg, string entity, UserId userId, IDbTransaction? outsideTransaction = null)
    {
        return SaveEventsInternal(newAgg, oldAgg?.Version ?? 0, entity, userId);
    }

    public Task<FSharpOption<T>> Get<T>(string key)
    {
        var result = _blobs.ContainsKey(key) switch
        {
            true => FSharpOption<T>.Some((T)_blobs[key]),
            false => FSharpOption<T>.None
        };
        return Task.FromResult(result);
    }

    public Task Save<T>(string key, T t)
    {
        if (t == null)
        {
            throw new ArgumentNullException(nameof(t));
        }
        
        _blobs[key] = t;
        return Task.CompletedTask;
    }
}
