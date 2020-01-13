using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Shared;
using StackExchange.Redis;

namespace storage.redis
{
    public class AggregateStorage
    {
        protected ConnectionMultiplexer _redis;

        public AggregateStorage(string redisCnn)
        {
            _redis = ConnectionMultiplexer.Connect(redisCnn);
        }

        public async Task StoreEventsMigration(IEnumerable<storage.postgres.StoredAggregateEvent> events)
        {
            var db = _redis.GetDatabase();

            foreach (var e in events)
            {
                var globalKey = $"{e.Entity}:{e.UserId}";
                var entityKey = $"{e.Entity}:{e.UserId}:{e.Key}";
                var keyToStore = $"{e.Entity}:{e.UserId}:{e.Key}:{e.Version}";

                await db.SetAddAsync(globalKey, keyToStore);
                await db.SetAddAsync(entityKey, keyToStore);

                var fields = new HashEntry[] {
                    new HashEntry("created", e.Created.ToString("o")),
                    new HashEntry("entity", e.Entity),
                    new HashEntry("event", e.EventJson),
                    new HashEntry("key", e.Key),
                    new HashEntry("userId", e.UserId),
                    new HashEntry("version", e.Version),
                };

                await db.HashSetAsync(keyToStore, fields);
            }
        }

        protected async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string key, string userId)
        {
            var redisKey = entity + ":" + userId + ":" + key;

            var db = _redis.GetDatabase();

            var keys = await db.SetMembersAsync(redisKey);

            return keys.Select(async k => await db.HashGetAllAsync(k.ToString()))
                .Select(e => ToEvent(entity, userId, e.Result))
                .OrderBy(e => e.Version)
                .Select(e => e.Event);
        }

        protected async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string userId)
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

        protected async Task SaveEventsAsync(core.Shared.Aggregate agg, string entity)
        {
            var db = _redis.GetDatabase();

            int version = agg.Version;

            foreach (var e in agg.Events.Skip(agg.Version))
            {
                var se = new storage.postgres.StoredAggregateEvent
                {
                    Entity = entity,
                    Event = e,
                    Key = e.Ticker,
                    UserId = e.UserId,
                    Created = e.When,
                    Version = ++version
                };

                var globalKey = $"{entity}:{e.UserId}";
                var entityKey = $"{entity}:{e.UserId}:{e.Ticker}";
                var keyToStore = $"{entity}:{e.UserId}:{e.Ticker}:{version}";

                await db.SetAddAsync(globalKey, keyToStore);
                await db.SetAddAsync(entityKey, keyToStore);

                var fields = new HashEntry[] {
                    new HashEntry("created", e.When.ToString("o")),
                    new HashEntry("entity", entity),
                    new HashEntry("event", se.EventJson),
                    new HashEntry("key", e.Ticker),
                    new HashEntry("userId", e.UserId),
                    new HashEntry("version", version),
                };

                await db.HashSetAsync(keyToStore, fields);
            }
        }

        private storage.postgres.StoredAggregateEvent ToEvent(string entity, string userId, HashEntry[] result)
        {
            return new storage.postgres.StoredAggregateEvent {
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
    }
}