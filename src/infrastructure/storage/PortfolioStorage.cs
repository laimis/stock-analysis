using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;
using Dapper;

namespace storage
{
    public class PortfolioStorage : AggregateStorage, IPortfolioStorage
    {
        public PortfolioStorage(string cnn) : base(cnn)
        {
        }

        public async Task<OwnedStock> GetStock(string ticker, string userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = 'ownedstock' AND key = :ticker AND userId = :userId ORDER BY version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { ticker, userId });

                var events = list.Select(e => e.Event).ToList();

                if (events.Count == 0)
                {
                    return null;
                }

                return new OwnedStock(events);
            }
        }

        public async Task Save(OwnedStock stock)
        {
            using (var db = GetConnection())
            {
                db.Open();

                int version = stock.Version;

                using (var tx = db.BeginTransaction())
                {
                    foreach (var e in stock.Events.Skip(stock.Version))
                    {
                        var se = new StoredAggregateEvent
                        {
                            Entity = "ownedstock",
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

        public async Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            using (var db = GetConnection())
            {
                db.Open();

                var query = @"select * FROM events WHERE entity = 'ownedstock' AND userId = :userId ORDER BY version";

                var list = await db.QueryAsync<StoredAggregateEvent>(query, new { userId });

                return list.GroupBy(e => e.Key)
                    .Select(g => new OwnedStock(g.Select(ag => ag.Event).ToList()));
            }
        }
    }
}