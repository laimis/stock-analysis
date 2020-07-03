using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.Shared;
using Dapper;
using MediatR;
using Npgsql;
using storage.shared;

namespace storage.postgres
{
    public class PostgresAggregateStorage : IAggregateStorage
    {
        private IMediator _mediator;
        protected string _cnn;

        public PostgresAggregateStorage(
            IMediator mediator,
            string cnn)
        {
            _mediator = mediator;
            _cnn = cnn;
        }

        protected IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_cnn);
        }

        public async Task DeleteAggregates(string entity, Guid userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"DELETE FROM events WHERE entity = :entity AND userId = :userId";

                await db.ExecuteAsync(query, new { userId, entity });
            }
        }

        public async Task<IEnumerable<AggregateEvent>> GetEventsAsync(string entity, Guid userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = :entity AND userId = :userId ORDER BY version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { entity, userId });

                return list.Select(e => e.Event);
            }
        }

        public async Task SaveEventsAsync(Aggregate agg, string entity, Guid userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                int version = agg.Version;

                var eventsToBlast = new List<AggregateEvent>();

                using (var tx = db.BeginTransaction())
                {
                    foreach (var e in agg.Events.Skip(agg.Version))
                    {
                        var se = new StoredAggregateEvent
                        {
                            Entity = entity,
                            Event = e,
                            Key = e.Id.ToString(),
                            UserId = userId,
                            Created = DateTimeOffset.UtcNow,
                            Version = ++version
                        };

                        var query = @"INSERT INTO events (entity, key, userid, created, version, eventjson) VALUES
						(@Entity, @Key, @UserId, @Created, @Version, @EventJson)";

                        await db.ExecuteAsync(query, se);

                        eventsToBlast.Add(e);
                    }

                    tx.Commit();

                    foreach(var e in eventsToBlast)
                        if (e is INotification n)
                            await _mediator.Publish(n);
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

        public async Task<IEnumerable<StoredAggregateEvent>> GetStoredEvents(string entity, Guid userId)
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