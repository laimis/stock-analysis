using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Shared;
using StackExchange.Redis;
using storage.shared;

namespace storage.redis
{
    public class AggregateStorage : IAggregateStorage
    {
        protected ConnectionMultiplexer _redis;

        public AggregateStorage(string redisCnn)
        {
            _redis = ConnectionMultiplexer.Connect(redisCnn);
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string userId)
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

        public async Task SaveEventsAsync(core.Shared.Aggregate agg, string entity, string userId)
        {
            var db = _redis.GetDatabase();

            int version = agg.Version;

            var tx = db.CreateTransaction();

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
                    new HashEntry("userId", userId),
                    new HashEntry("version", version),
                };

                await tx.SetAddAsync(globalKey, keyToStore);
                await tx.SetAddAsync(entityKey, keyToStore);
                await tx.HashSetAsync(keyToStore, fields);
            }

            var success = await tx.ExecuteAsync();

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Redis transaction failed saving aggregate {entity}, {agg.Id}"
                );
            }
        }

        internal static storage.shared.StoredAggregateEvent ToEvent(string entity, string userId, HashEntry[] result)
        {
            var eventJson = result.Single(h => h.Name == "event").Value;

            try
            {
                return new storage.shared.StoredAggregateEvent {
                    Created = DateTime.Parse(result.Single(h => h.Name == "created").Value, null, System.Globalization.DateTimeStyles.AssumeUniversal),
                    Entity = result.Single(h => h.Name == "entity").Value,
                    EventJson = eventJson,
                    Key = result.Single(h => h.Name == "key").Value,
                    UserId = result.Single(h => h.Name == "userId").Value,
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

        public async Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, string userId)
        {
            var redisKey = entity + ":" + userId;

            var db = _redis.GetDatabase();

            var keys = await db.SetMembersAsync(redisKey);

            return keys.Select(async k => await db.HashGetAllAsync(k.ToString()))
                .Select(e => ToEvent(entity, userId, e.Result))
                .OrderBy(e => e.Key)
                .ThenBy(e => e.Version);
        }
    }
}