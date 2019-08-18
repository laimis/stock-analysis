using System.Data;
using System.Linq;
using System.Threading.Tasks;
using analysis;
using Dapper;
using financialmodelingclient;
using Newtonsoft.Json;
using Npgsql;

namespace storage
{
	public class AnalysisStorage : IAnalysisStorage
	{
		public Task SaveAnalysisAsync(
			string ticker,
			MetricsResponse metrics,
			HistoricalResponse prices,
			CompanyProfile company)
		{
			using (var db = GetConnection())
			{
				db.Open();

				InsertAnalysis(ticker, metrics, prices, db);

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
			query = @"INSERT INTO companies (ticker, created, json) VALUES (:ticker, current_timestamp, :json)";
			var obj = new {
				ticker = ticker, json = JsonConvert.SerializeObject(company)
			};
			db.Execute(query, obj);
		}

		private void InsertPrices(string ticker, HistoricalResponse prices, IDbConnection db)
		{
			var query = "DELETE FROM prices WHERE ticker = :ticker";
			db.Execute(query, new {ticker});
			query = @"INSERT INTO prices (ticker, created, json) VALUES (:ticker, current_timestamp, :json)";
			var obj = new {
				ticker = ticker, json = JsonConvert.SerializeObject(prices)
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

		private static void InsertAnalysis(string ticker, MetricsResponse metrics, HistoricalResponse prices, IDbConnection db)
		{
			var query = "DELETE FROM analysis WHERE ticker = :ticker";
			db.Execute(query, new { ticker });
			query = @"INSERT INTO analysis (ticker, created, lastprice, lastbookvalue, lastpevalue)
				VALUES (:ticker, current_timestamp, :lastprice, :lastbookvalue, :lastpevalue)";

			var lastprice = prices.Historical.LastOrDefault();
			var lastbookvalue = metrics.Metrics.FirstOrDefault()?.BookValuePerShare;
			var lastpevalue = metrics.Metrics.FirstOrDefault()?.PERatio;

			var obj = new { ticker, lastprice = lastprice?.Close, lastbookvalue, lastpevalue };

			db.Execute(query, obj);
		}

		private IDbConnection GetConnection()
		{
			return new NpgsqlConnection("Server=localhost;Database=stocks;User id=stocks;password=stocks");
		}
	}
}