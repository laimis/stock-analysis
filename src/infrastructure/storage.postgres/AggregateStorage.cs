using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.Shared;
using Dapper;
using Npgsql;
using storage.shared;

namespace storage.postgres
{
    public class AggregateStorage : IAggregateStorage
    {
        protected string _cnn;

        public AggregateStorage(string cnn)
        {
            _cnn = cnn;
        }

        protected IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_cnn);
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string key, string userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = :entity AND key = :key AND userId = :userId ORDER BY version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { entity, key, userId });

                return list.Select(e => e.Event);
            }
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, string userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = :entity AND userId = :userId ORDER BY key, version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { entity, userId });

                return list.Select(e => e.Event);
            }
        }

        public async Task SaveEventsAsync(Aggregate agg, string entity)
        {
            using (var db = GetConnection())
            {
                db.Open();

                int version = agg.Version;

                using (var tx = db.BeginTransaction())
                {
                    foreach (var e in agg.Events.Skip(agg.Version))
                    {
                        var se = new StoredAggregateEvent
                        {
                            Entity = entity,
                            Event = e,
                            Key = e.Ticker,
                            UserId = e.UserId,
                            Created = e.When,
                            Version = ++version
                        };

                        var query = @"INSERT INTO events (entity, key, userid, created, version, eventjson) VALUES
						(@Entity, @Key, @UserId, @Created, @Version, @EventJson)";

                        await db.ExecuteAsync(query, se);
                    }

                    tx.Commit();
                }
            }
        }

        public async Task DoHealthCheck()
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select 1";

                await db.QueryAsync<int>(query);
            }
        }

        public async Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, string userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = :entity AND userId = :userId ORDER BY key, version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { entity, userId });

                return list;
            }
        }
    }
}