using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.Shared;
using Dapper;
using Microsoft.FSharp.Core;
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

        public async Task DeleteAggregates(string entity, UserId userId, IDbTransaction? outsideTransaction = null)
        {
            using var db = outsideTransaction?.Connection ?? GetConnection();
            using var tx = outsideTransaction ?? db.BeginTransaction();
            await db.ExecuteAsync(
                @"DELETE FROM events WHERE entity = :entity AND userId = :userId",
                new { userId = userId.Item, entity }
            );
            tx.Commit();
        }

        public async Task DeleteAggregate(string entity, Guid aggregateId, UserId userId)
        {
            using var db = GetConnection();
            // TODO: no aggregate id in the column, huh?
            // might want to do a migration and add one...
            await db.ExecuteAsync(
                @"DELETE FROM events WHERE entity = :entity AND userId = :userId AND aggregateId = :aggregateId",
                new { userId = userId.Item, entity, aggregateId = aggregateId.ToString() }
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
        
        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid aggregateId, UserId userId)
        {
            using var db = GetConnection();
            var list = await db.QueryAsync<StoredAggregateEvent>(
                @"select * FROM events WHERE entity = :entity AND userId = :userId AND aggregateId = :aggregateId ORDER BY version",
                new { entity, userId = userId.Item, aggregateId = aggregateId.ToString() }
            );

            return list.Select(e => e.Event);
        }

        private async Task SaveEventsAsyncInternal(IAggregate agg, int fromVersion, string entity, UserId userId,
            IDbTransaction? outsideTransaction = null)
        {
            using var db = outsideTransaction?.Connection ?? GetConnection();
            int version = fromVersion;

            var eventsToBlast = new List<AggregateEvent>();

            using var tx = outsideTransaction ?? db.BeginTransaction();
            foreach (var e in agg.Events.Skip(fromVersion))
            {
                var se = new StoredAggregateEvent
                {
                    Entity = entity,
                    Event = e,
                    Key = e.Id.ToString(),
                    UserId = userId.Item,
                    AggregateId = e.AggregateId.ToString(),
                    Created = DateTimeOffset.UtcNow,
                    Version = ++version
                };

                var query = @"INSERT INTO events (entity, key, aggregateid, userid, created, version, eventjson) VALUES
						(@Entity, @Key, @AggregateId, @UserId, @Created, @Version, @EventJson)";

                await db.ExecuteAsync(query, se);

                eventsToBlast.Add(e);
            }

            await _outbox.AddEvents(eventsToBlast, tx);

            tx.Commit();
        }

        public Task SaveEventsAsync(IAggregate agg, string entity, UserId userId, IDbTransaction? outsideTransaction = null)
        {
            return SaveEventsAsyncInternal(agg, agg.Version, entity, userId, outsideTransaction);
        }

        public Task SaveEventsAsync(IAggregate? oldAggregate, IAggregate newAggregate, string entity, UserId userId, IDbTransaction? outsideTransaction = null)
        {
            return SaveEventsAsyncInternal(newAggregate, oldAggregate?.Version ?? 0, entity, userId, outsideTransaction);
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

        public async Task<FSharpOption<T>> Get<T>(string key)
        {
            using var db = GetConnection();

            var blob = await db.QuerySingleOrDefaultAsync<string>(
                @"select blob FROM blobs WHERE key = :key",
                new { key }
            );

            return string.IsNullOrEmpty(blob) ? 
                FSharpOption<T>.None :
                FSharpOption<T>.Some(JsonConvert.DeserializeObject<T>(blob)!);
        }

        public async Task Save<T>(string key, T t)
        {
            var blob = JsonConvert.SerializeObject(t);

            using var db = GetConnection();

            await db.ExecuteAsync(
                @"INSERT INTO blobs (key, blob, inserted) VALUES (@key, @blob, current_timestamp) ON CONFLICT (key) DO UPDATE SET blob = @blob, inserted = current_timestamp",
                new { key, blob }
            );
        }
    }
}
