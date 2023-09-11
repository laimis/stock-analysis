using core.Shared;
using storage.shared;

namespace storage.memory;

public class MemoryAggregateStorage : IAggregateStorage, IBlobStorage
{
    public MemoryAggregateStorage(IOutbox outbox)
    {
        _outbox = outbox;
    }
    
    private static readonly Dictionary<string, List<StoredAggregateEvent>> _aggregates = new();
    
    private static readonly Dictionary<string, object> _blobs = new();
    private readonly IOutbox _outbox;

    public Task DeleteAggregates(string entity, Guid userId)
    {
        _aggregates.Remove(MakeKey(entity, userId));
        return Task.CompletedTask;
    }

    public Task DeleteAggregate(string entity, Guid aggregateId, Guid userId)
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

    private string MakeKey(string entity, Guid aggregateId) => $"{entity}-{aggregateId}";

    public Task DoHealthCheck() => Task.CompletedTask;

    public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid userId)
    {
        var events = await GetStoredEvents(entity, userId);

        return events.Select(e => e.Event);
    }

    public Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, Guid userId)
    {
        var key = MakeKey(entity, userId);
        if (!_aggregates.ContainsKey(key))
        {
            return Task.FromResult(Enumerable.Empty<StoredAggregateEvent>());
        }
        return Task.FromResult(_aggregates[key].AsEnumerable());
    }

    public async Task SaveEventsAsync(Aggregate agg, string entity, Guid userId)
    {
        var key = MakeKey(entity, userId);

        if (!_aggregates.ContainsKey(key))
        {
            _aggregates[key] = new List<StoredAggregateEvent>();
        }

        var version = agg.Version;

        var eventsToBlast = new List<AggregateEvent>();

        foreach (var e in agg.Events.Skip(agg.Version))
        {
            var se = new StoredAggregateEvent
            {
                Entity = entity,
                Event = e,
                Key = e.Id.ToString(),
                UserId = userId,
                Created = e.When,
                Version = ++version
            };

            _aggregates[key].Add(se);
            eventsToBlast.Add(e);
        }

        await _outbox.AddEvents(eventsToBlast, null);
    }

    public Task<T> Get<T>(string key) => Task.FromResult((T)_blobs[key]);

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