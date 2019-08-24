using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using core;
using Dapper;
using financialmodelingclient;
using Newtonsoft.Json;
using Npgsql;

namespace storage
{
	public class AnalysisStorage : IAnalysisStorage
	{
		private string _cnn;

		public AnalysisStorage(string cnn)
		{
			_cnn = cnn;
		}
		
		public async Task<IEnumerable<AnalysisInfo>> GetAnalysisAsync()
		{
			using (var db = GetConnection())
			{
				db.Open();

				var query = @"select * FROM analysis ORDER BY ticker";

				return await db.QueryAsync<AnalysisInfo>(query);
			}
		}

		public async Task<AnalysisInfo> GetAnalysisAsync(string ticker)
		{
			using (var db = GetConnection())
			{
				db.Open();

				var query = @"select * FROM analysis WHERE ticker = :ticker";

				return await db.QuerySingleOrDefaultAsync<AnalysisInfo>(query, new {ticker});
			}
		}

		public Task SaveAnalysisAsync(
			string ticker,
			MetricsResponse metrics,
			HistoricalResponse prices,
			CompanyProfile company)
		{
			using (var db = GetConnection())
			{
				db.Open();

				InsertAnalysis(ticker, metrics, prices, company, db);

				InsertMetrics(ticker, metrics, db);

				InsertPrices(ticker, prices, db);

				InsertProfile(ticker, company, db);

				return Task.CompletedTask;
			}
		}

		private void InsertProfile(string ticker, CompanyProfile company, IDbConnection db)
		{
			var query = "DELETE FROM companies WHERE ticker = :ticker";
			db.Execute(query, new {ticker});
			query = @"INSERT INTO companies (ticker, created, name, industry, sector, json) VALUES (:ticker, current_timestamp, :name, :industry, :sector, :json)";
			var obj = new {
				ticker = ticker,
				name = company.Profile.CompanyName,
				industry = company.Profile.Industry,
				sector = company.Profile.Sector,
				json = JsonConvert.SerializeObject(company)
			};
			db.Execute(query, obj);
		}

		private void InsertPrices(string ticker, HistoricalResponse prices, IDbConnection db)
		{
			var query = "DELETE FROM prices WHERE ticker = :ticker";
			db.Execute(query, new {ticker});
			query = @"INSERT INTO prices (ticker, created, lastclose, json) VALUES (:ticker, current_timestamp, :lastclose, :json)";
			var obj = new {
				ticker = ticker,
				lastclose = prices.Historical.LastOrDefault().Close,
				json = JsonConvert.SerializeObject(prices)
			};
			db.Execute(query, obj);
		}

		private void InsertMetrics(string ticker, MetricsResponse metrics, IDbConnection db)
		{
			var query = "DELETE FROM metrics WHERE ticker = :ticker";
			db.Execute(query, new {ticker});
			query = @"INSERT INTO metrics (ticker, created, json) VALUES (:ticker, current_timestamp, :json)";
			var obj = new {
				ticker = ticker, json = JsonConvert.SerializeObject(metrics)
			};
			db.Execute(query, obj);
		}

		private static void InsertAnalysis(string ticker, MetricsResponse metrics, HistoricalResponse prices, CompanyProfile profile, IDbConnection db)
		{
			var query = "DELETE FROM analysis WHERE ticker = :ticker";
			db.Execute(query, new { ticker });
			query = @"INSERT INTO analysis (ticker, created, lastprice, lastbookvalue, lastpevalue, industry)
				VALUES (:ticker, current_timestamp, :lastprice, :lastbookvalue, :lastpevalue, :industry)";

			var lastprice = prices.Historical.LastOrDefault();
			var lastbookvalue = metrics.Metrics.FirstOrDefault()?.BookValuePerShare;
			var lastpevalue = metrics.Metrics.FirstOrDefault()?.PERatio;
			var industry = profile.Profile.Industry;

			var obj = new { ticker, lastprice = lastprice?.Close, lastbookvalue, lastpevalue, industry };

			db.Execute(query, obj);
		}

		private IDbConnection GetConnection()
		{
			return new NpgsqlConnection(_cnn);
		}
	}
}