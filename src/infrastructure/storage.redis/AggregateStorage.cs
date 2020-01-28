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
                .OrderBy(e => e.Key)
                .ThenBy(e => e.Version)
                .Select(e => e.Event);
        }

        public async Task SaveEventsAsync(core.Shared.Aggregate agg, string entity, string userId)
        {
            var db = _redis.GetDatabase();

            int version = agg.Version;

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

                await db.SetAddAsync(globalKey, keyToStore);
                await db.SetAddAsync(entityKey, keyToStore);

                var fields = new HashEntry[] {
                    new HashEntry("created", e.When.ToString("o")),
                    new HashEntry("entity", entity),
                    new HashEntry("event", se.EventJson),
                    new HashEntry("key", e.Id.ToString()),
                    new HashEntry("userId", userId),
                    new HashEntry("version", version),
                };

                await db.HashSetAsync(keyToStore, fields);
            }
        }

        internal static storage.shared.StoredAggregateEvent ToEvent(string entity, string userId, HashEntry[] result)
        {
            return new storage.shared.StoredAggregateEvent {
                Created = DateTime.Parse(result.Single(h => h.Name == "created").Value, null, System.Globalization.DateTimeStyles.AssumeUniversal),
                Entity = result.Single(h => h.Name == "entity").Value,
                EventJson = result.Single(h => h.Name == "event").Value,
                Key = result.Single(h => h.Name == "key").Value,
                UserId = result.Single(h => h.Name == "userId").Value,
                Version = int.Parse(result.Single(h => h.Name == "version").Value),
            };
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