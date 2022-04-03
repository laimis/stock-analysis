using core.Shared;
using MediatR;
using storage.shared;

namespace storage.memory;

public class MemoryAggregateStorage : IAggregateStorage, IBlobStorage
{
    public MemoryAggregateStorage(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    private static Dictionary<string, List<StoredAggregateEvent>> _aggregates = 
        new Dictionary<string, List<StoredAggregateEvent>>();
    protected IMediator _mediator;
    private static Dictionary<string, object> _blobs = new Dictionary<string, object>();

    public Task DeleteAggregates(string entity, Guid aggregateId)
    {
        _aggregates.Remove(MakeKey(entity, aggregateId));
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
            var se = new storage.shared.StoredAggregateEvent
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

        foreach(var e in eventsToBlast)
            if (e is INotification n)
                await _mediator.Publish(n);

        if (eventsToBlast.Count > 0)
        {
            await _mediator.Publish(new ScheduleUserChanged(userId));
        }
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