using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Storage;
using core.Shared;

namespace storage.memory
{
    public class SECFilingStorage : ISECFilingStorage
    {
        private static readonly Dictionary<string, SECFilingRecord> _filingsByUrl = new();
        private static readonly List<SECFilingRecord> _filings = new();
        private static readonly object _lock = new();

        public Task<bool> SaveFiling(SECFilingRecord filing)
        {
            lock (_lock)
            {
                if (_filingsByUrl.ContainsKey(filing.FilingUrl))
                {
                    return Task.FromResult(false);
                }

                _filingsByUrl[filing.FilingUrl] = filing;
                _filings.Add(filing);
                return Task.FromResult(true);
            }
        }

        public Task<int> SaveFilings(IEnumerable<SECFilingRecord> filings)
        {
            lock (_lock)
            {
                var count = 0;
                foreach (var filing in filings)
                {
                    if (!_filingsByUrl.ContainsKey(filing.FilingUrl))
                    {
                        _filingsByUrl[filing.FilingUrl] = filing;
                        _filings.Add(filing);
                        count++;
                    }
                }
                return Task.FromResult(count);
            }
        }

        public Task<IEnumerable<SECFilingRecord>> GetFilingsByTicker(Ticker ticker)
        {
            lock (_lock)
            {
                var results = _filings
                    .Where(f => f.Ticker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.FilingDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<SECFilingRecord>>(results);
            }
        }

        public Task<IEnumerable<SECFilingRecord>> GetRecentFilingsByTicker(Ticker ticker, int days)
        {
            lock (_lock)
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
                
                var results = _filings
                    .Where(f => f.Ticker.Equals(ticker.Value, StringComparison.OrdinalIgnoreCase) 
                             && string.Compare(f.FilingDate, cutoffDate, StringComparison.Ordinal) >= 0)
                    .OrderByDescending(f => f.FilingDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<SECFilingRecord>>(results);
            }
        }

        public Task<IEnumerable<SECFilingRecord>> GetFilingsByTickers(IEnumerable<Ticker> tickers, int days)
        {
            lock (_lock)
            {
                var tickerSet = new HashSet<string>(
                    tickers.Select(t => t.Value),
                    StringComparer.OrdinalIgnoreCase
                );
                
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
                
                var results = _filings
                    .Where(f => tickerSet.Contains(f.Ticker) 
                             && string.Compare(f.FilingDate, cutoffDate, StringComparison.Ordinal) >= 0)
                    .OrderBy(f => f.Ticker)
                    .ThenByDescending(f => f.FilingDate)
                    .ToList();
                
                return Task.FromResult<IEnumerable<SECFilingRecord>>(results);
            }
        }

        public Task<bool> FilingExists(string filingUrl)
        {
            lock (_lock)
            {
                return Task.FromResult(_filingsByUrl.ContainsKey(filingUrl));
            }
        }

        public Task<IEnumerable<SECFilingRecord>> GetFilingsByFormType(IEnumerable<string> formTypes, int limit)
        {
            lock (_lock)
            {
                var formTypeSet = new HashSet<string>(formTypes, StringComparer.OrdinalIgnoreCase);
                
                var results = _filings
                    .Where(f => formTypeSet.Contains(f.FormType))
                    .OrderByDescending(f => f.FilingDate)
                    .Take(limit)
                    .ToList();
                
                return Task.FromResult<IEnumerable<SECFilingRecord>>(results);
            }
        }
    }
}
