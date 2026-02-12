using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.Shared;
using Dapper;
using Npgsql;

namespace storage.postgres
{
    public class SECFilingStorage : ISECFilingStorage
    {
        private readonly string _connectionString;

        public SECFilingStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<bool> SaveFiling(SECFilingRecord filing)
        {
            using var db = GetConnection();
            
            var query = @"
                INSERT INTO sec_filings (id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at)
                VALUES (@Id, @Ticker, @Cik, @FormType, @FilingDate, @ReportDate, @Description, @FilingUrl, @DocumentUrl, @CreatedAt)
                ON CONFLICT (filing_url) DO NOTHING";

            var rowsAffected = await db.ExecuteAsync(query, new
            {
                Id = filing.Id,
                Ticker = filing.Ticker,
                Cik = filing.Cik,
                FormType = filing.FormType,
                FilingDate = filing.FilingDate,
                ReportDate = filing.ReportDate,
                Description = filing.Description,
                FilingUrl = filing.FilingUrl,
                DocumentUrl = filing.DocumentUrl,
                CreatedAt = filing.CreatedAt
            });

            return rowsAffected > 0;
        }

        public async Task<int> SaveFilings(IEnumerable<SECFilingRecord> filings)
        {
            using var db = GetConnection();
            
            var query = @"
                INSERT INTO sec_filings (id, ticker, cik, form_type, filing_date, report_date, description, filing_url, document_url, created_at)
                VALUES (@Id, @Ticker, @Cik, @FormType, @FilingDate, @ReportDate, @Description, @FilingUrl, @DocumentUrl, @CreatedAt)
                ON CONFLICT (filing_url) DO NOTHING";

            var filingsArray = filings.ToArray();
            var rowsAffected = await db.ExecuteAsync(query, filingsArray);

            return rowsAffected;
        }

        public async Task<IEnumerable<SECFilingRecord>> GetFilingsByTicker(Ticker ticker)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                       filing_date as FilingDate, report_date as ReportDate, description as Description,
                       filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt
                FROM sec_filings
                WHERE ticker = @Ticker
                ORDER BY filing_date DESC";

            var results = await db.QueryAsync<SECFilingRecord>(query, new { Ticker = ticker.Value });
            return results;
        }

        public async Task<IEnumerable<SECFilingRecord>> GetRecentFilingsByTicker(Ticker ticker, int days)
        {
            using var db = GetConnection();
            
            // Calculate the cutoff date
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            
            var query = @"
                SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                       filing_date as FilingDate, report_date as ReportDate, description as Description,
                       filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt
                FROM sec_filings
                WHERE ticker = @Ticker AND filing_date >= @CutoffDate
                ORDER BY filing_date DESC";

            var results = await db.QueryAsync<SECFilingRecord>(query, new { Ticker = ticker.Value, CutoffDate = cutoffDate });
            return results;
        }

        public async Task<IEnumerable<SECFilingRecord>> GetFilingsByTickers(IEnumerable<Ticker> tickers, int days)
        {
            var tickerValues = tickers.Select(t => t.Value).ToArray();
            
            if (!tickerValues.Any())
                return Enumerable.Empty<SECFilingRecord>();

            using var db = GetConnection();
            
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            
            var query = @"
                SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                       filing_date as FilingDate, report_date as ReportDate, description as Description,
                       filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt
                FROM sec_filings
                WHERE ticker = ANY(@Tickers) AND filing_date >= @CutoffDate
                ORDER BY ticker, filing_date DESC";

            var results = await db.QueryAsync<SECFilingRecord>(query, new { Tickers = tickerValues, CutoffDate = cutoffDate });
            return results;
        }

        public async Task<bool> FilingExists(string filingUrl)
        {
            using var db = GetConnection();
            
            var query = @"
                SELECT EXISTS(SELECT 1 FROM sec_filings WHERE filing_url = @FilingUrl)";

            var exists = await db.ExecuteScalarAsync<bool>(query, new { FilingUrl = filingUrl });
            return exists;
        }

        public async Task<IEnumerable<SECFilingRecord>> GetFilingsByFormType(IEnumerable<string> formTypes, int limit)
        {
            var formTypesArray = formTypes.ToArray();
            
            if (!formTypesArray.Any())
                return Enumerable.Empty<SECFilingRecord>();

            using var db = GetConnection();
            
            var query = @"
                SELECT id as Id, ticker as Ticker, cik as Cik, form_type as FormType, 
                       filing_date as FilingDate, report_date as ReportDate, description as Description,
                       filing_url as FilingUrl, document_url as DocumentUrl, created_at as CreatedAt
                FROM sec_filings
                WHERE form_type = ANY(@FormTypes)
                ORDER BY filing_date DESC
                LIMIT @Limit";

            var results = await db.QueryAsync<SECFilingRecord>(query, new { FormTypes = formTypesArray, Limit = limit });
            return results;
        }
    }
}
