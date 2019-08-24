using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;
using Dapper;
using Npgsql;

namespace storage
{
	public class PortfolioStorage : IPortfolioStorage
	{
		private string _cnn;

		public PortfolioStorage(string cnn)
		{
			_cnn = cnn;
		}

		private IDbConnection GetConnection()
		{
			return new NpgsqlConnection(_cnn);
		}

		public async Task<OwnedStock> GetStock(string ticker, string userId)
		{
			using (var db = GetConnection())
			{
				db.Open();

				var query = @"select * FROM ownedstocks WHERE ticker = :ticker AND userId = :userId ORDER BY version";

				var list = await db.QueryAsync<StoredAggregateEvent>(query, new {ticker, userId});

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
					foreach(var e in stock.Events.Skip(stock.Version))
					{
						var se = new StoredAggregateEvent{
							Event = e,
							Ticker = e.Ticker,
							UserId = e.UserId,
							Created = e.When,
							Version = ++version
						};

						var query = @"INSERT INTO ownedstocks (ticker, userid, created, version, eventjson) VALUES
						(@Ticker, @UserId, @Created, @Version, @EventJson)";

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

				var query = @"select * FROM ownedstocks WHERE userId = :userId ORDER BY version";

				var list = await db.QueryAsync<StoredAggregateEvent>(query, new {userId});

				return list.GroupBy(e => e.Ticker)
					.Select(g => new OwnedStock(g.Select(ag => ag.Event).ToList()));
			}
		}
	}
}