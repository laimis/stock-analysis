using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Shared;
using MediatR;
using Newtonsoft.Json;
using StackExchange.Redis;
using storage.shared;

namespace storage.redis
{
    public class RedisAggregateStorage : IAggregateStorage, IBlobStorage
    {
        protected IMediator _mediator;
        protected ConnectionMultiplexer _redis;

        public RedisAggregateStorage(IMediator mediator, string redisCnn)
        {
            _mediator = mediator;
            _redis = ConnectionMultiplexer.Connect(redisCnn);
        }

        public async Task<T> Get<T>(string key)
        {
            var redisKey = typeof(T).Name + ":" + key;

            var db = _redis.GetDatabase();

            var val = await db.StringGetAsync(redisKey);

            return val.HasValue ? JsonConvert.DeserializeObject<T>(val) : default(T);
        }

        public Task Save<T>(string key, T t)
        {
            var redisKey = typeof(T).Name + ":" + key;

            var db = _redis.GetDatabase();

            return db.StringSetAsync(redisKey, JsonConvert.SerializeObject(t));
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid userId)
        {
            var redisKey = entity + ":" + userId;

            var db = _redis.GetDatabase();

            var keys = await db.SetMembersAsync(redisKey);

            return keys.Select(async k => await db.HashGetAllAsync(k.ToString()))
                .Select(e => ToEvent(entity, userId, e.Result))
                .OrderBy(e => e.Event.AggregateId)
                .ThenBy(e => e.Version)
                .Select(e => e.Event);
        }

        public async Task SaveEventsAsync(core.Shared.Aggregate agg, string entity, Guid userId)
        {
            var db = _redis.GetDatabase();

            int version = agg.Version;

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

                var globalKey = $"{entity}:{userId}";
                var entityKey = $"{entity}:{userId}:{e.Id}";
                var keyToStore = $"{entity}:{userId}:{e.Id}:{version}";

                var fields = new HashEntry[] {
                    new HashEntry("created", e.When.ToString("o")),
                    new HashEntry("entity", entity),
                    new HashEntry("event", se.EventJson),
                    new HashEntry("key", e.Id.ToString()),
                    new HashEntry("userId", userId.ToString()),
                    new HashEntry("version", version),
                };

                await db.SetAddAsync(globalKey, keyToStore);
                await db.SetAddAsync(entityKey, keyToStore);
                await db.HashSetAsync(keyToStore, fields);

                eventsToBlast.Add(e);
            }

            foreach(var e in eventsToBlast)
                if (e is INotification n)
                    await _mediator.Publish(n);

            if (eventsToBlast.Count > 0)
                await _mediator.Publish(new UserRecalculate(userId));
        }

        internal static storage.shared.StoredAggregateEvent ToEvent(string entity, Guid userId, HashEntry[] result)
        {
            var eventJson = result.Single(h => h.Name == "event").Value;

            try
            {
                return new storage.shared.StoredAggregateEvent {
                    Created = DateTime.Parse(result.Single(h => h.Name == "created").Value, null, System.Globalization.DateTimeStyles.AssumeUniversal),
                    Entity = result.Single(h => h.Name == "entity").Value,
                    EventJson = eventJson,
                    Key = result.Single(h => h.Name == "key").Value,
                    UserId = new Guid(result.Single(h => h.Name == "userId").Value.ToString()),
                    Version = int.Parse(result.Single(h => h.Name == "version").Value),
                };
            }
            catch(InvalidOperationException ex)
            {
                throw new Exception("Failed to build event from JSON: " + eventJson, ex);
            }
        }

        public Task DoHealthCheck()
        {
            _redis.GetStatus();

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, Guid userId)
        {
            var redisKey = entity + ":" + userId;

            var db = _redis.GetDatabase();

            var keys = await db.SetMembersAsync(redisKey);

            return keys.Select(async k => await db.HashGetAllAsync(k.ToString()))
                .Select(e => ToEvent(entity, userId, e.Result))
                .OrderBy(e => e.Key)
                .ThenBy(e => e.Version);
        }

        public async Task DeleteAggregates(string entity, Guid userId)
        {
            var db = _redis.GetDatabase();

            var globalKey = $"{entity}:{userId}";

            // var globalKey = $"{entity}:{userId}";
            // var entityKey = $"{entity}:{userId}:{e.Id}";
            // var keyToStore = $"{entity}:{userId}:{e.Id}:{version}";

            var keys = await db.SetMembersAsync(globalKey);

            foreach(var k in keys)
            {
                var key = k.ToString();

                await db.KeyDeleteAsync(key);

                // get subset of a key, pointing to aggregate
                var aggInstanceKey = string.Join(":", key.Split(':').Take(3));

                await db.KeyDeleteAsync(aggInstanceKey);
            }

            await db.KeyDeleteAsync(globalKey);
        }
    }
}