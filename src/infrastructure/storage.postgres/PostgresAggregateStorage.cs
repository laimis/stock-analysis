using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Shared.Domain.Accounts;
using core.Shared;
using Dapper;
using Newtonsoft.Json;
using Npgsql;
using storage.shared;

namespace storage.postgres
{
    public class PostgresAggregateStorage : IAggregateStorage, IBlobStorage
    {
        private readonly string _cnn;
        private readonly IOutbox _outbox;

        public PostgresAggregateStorage(
            IOutbox outbox,
            string cnn)
        {
            _cnn = cnn;
            _outbox = outbox;
        }

        protected IDbConnection GetConnection()
        {
            var cnn = new NpgsqlConnection(_cnn);
            cnn.Open();
            return cnn; 
        }

        public async Task DeleteAggregates(string entity, UserId userId)
        {
            using (var db = GetConnection())
            {
                await db.ExecuteAsync(
                    @"DELETE FROM events WHERE entity = :entity AND userId = :userId",
                    new { userId = userId.Item, entity }
                );
            }
        }

        public async Task DeleteAggregate(string entity, Guid aggregateId, UserId userId)
        {
            using var db = GetConnection();
            // TODO: no aggregate id in the column, huh?
            // might want to do a migration and add one...
            await db.ExecuteAsync(
                @"DELETE FROM events WHERE entity = :entity AND userId = :userId AND eventjson LIKE :aggregateId",
                new { userId = userId.Item, entity, aggregateId = $"%{aggregateId}%" }
            );
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, UserId userId)
        {
            using var db = GetConnection();
            var list = await db.QueryAsync<StoredAggregateEvent>(
                @"select * FROM events WHERE entity = :entity AND userId = :userId ORDER BY version",
                new { entity, userId = userId.Item }
            );

            return list.Select(e => e.Event);
        }

        public async Task SaveEventsAsync(Aggregate agg, string entity, UserId userId)
        {
            using var db = GetConnection();
            int version = agg.Version;

            var eventsToBlast = new List<AggregateEvent>();

            using var tx = db.BeginTransaction();
            foreach (var e in agg.Events.Skip(agg.Version))
            {
                var se = new StoredAggregateEvent
                {
                    Entity = entity,
                    Event = e,
                    Key = e.Id.ToString(),
                    UserId = userId.Item,
                    Created = DateTimeOffset.UtcNow,
                    Version = ++version
                };

                var query = @"INSERT INTO events (entity, key, userid, created, version, eventjson) VALUES
						(@Entity, @Key, @UserId, @Created, @Version, @EventJson)";

                await db.ExecuteAsync(query, se);

                eventsToBlast.Add(e);
            }

            await _outbox.AddEvents(eventsToBlast, tx);

            tx.Commit();
        }

        public async Task DoHealthCheck()
        {
            using var db = GetConnection();
            await db.QueryAsync<int>(@"select 1");
        }

        public async Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, UserId userId)
        {
            using var db = GetConnection();
            var list = await db.QueryAsync<StoredAggregateEvent>(
                @"select * FROM events WHERE entity = :entity AND userId = :userId ORDER BY key, version",
                new { entity, userId = userId.Item }
            );

            return list;
        }

        public async Task<T> Get<T>(string key)
        {
            using var db = GetConnection();

            var blob = await db.QuerySingleOrDefaultAsync<string>(
                @"select blob FROM blobs WHERE key = :key",
                new { key }
            );

            if (string.IsNullOrEmpty(blob))
                return default;

            return JsonConvert.DeserializeObject<T>(blob);
        }

        public Task Save<T>(string key, T t)
        {
            var blob = JsonConvert.SerializeObject(t);

            using var db = GetConnection();

            return db.ExecuteAsync(
                @"INSERT INTO blobs (key, blob, inserted) VALUES (@key, @blob, current_timestamp)",
                new { key, blob }
            );
        }
    }
}