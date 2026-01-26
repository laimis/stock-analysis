using core.fs;
using core.fs.Adapters.SEC;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using SecuritiesExchangeCommission.Edgar;
using System.Text.Json;
using core.fs.Adapters.Storage;

namespace secedgar;

public class EdgarClient : ISECFilings, ISECTickerData
{
    private readonly ILogger<EdgarClient>? _logger;
    private readonly IAccountStorage? _accountStorage;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minimumRequestInterval = TimeSpan.FromMilliseconds(500); // Max 2 requests per second
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _appName;
    private readonly string _appVersion;
    private readonly string _email;

    public EdgarClient(ILogger<EdgarClient>? logger) : this(logger, "TradeWatch", "1.0", "support@tradewatch.io"){}
    
    public EdgarClient(ILogger<EdgarClient>? logger, IAccountStorage? accountStorage) : this(logger, accountStorage, "TradeWatch", "1.0", "support@tradewatch.io"){}

    public EdgarClient(ILogger<EdgarClient>? logger, string appName, string appVersion, string email)
    {
        _logger = logger;
        _appName = appName;
        _appVersion = appVersion;
        _email = email;
        
        // SEC requires all automated tools to identify themselves via User-Agent
        // This must be set before any EdgarSearch calls
        IdentificationManager.Instance.AppName = appName;
        IdentificationManager.Instance.AppVersion = appVersion;
        IdentificationManager.Instance.Email = email;
        
        ConfigureHttpClient();
    }
    
    public EdgarClient(ILogger<EdgarClient>? logger, IAccountStorage? accountStorage, string appName, string appVersion, string email)
    {
        _logger = logger;
        _accountStorage = accountStorage;
        _appName = appName;
        _appVersion = appVersion;
        _email = email;
        
        // SEC requires all automated tools to identify themselves via User-Agent
        // This must be set before any EdgarSearch calls
        IdentificationManager.Instance.AppName = appName;
        IdentificationManager.Instance.AppVersion = appVersion;
        IdentificationManager.Instance.Email = email;
        
        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        // Configure HttpClient with proper User-Agent for SEC API calls
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{_appName}/{_appVersion} ({_email})");
    }
        

    public async Task<FSharpResult<CompanyFilings,ServiceError>> GetFilings(Ticker symbol)
    {
        try
        {
            // Try to resolve ticker to CIK if we have account storage
            string searchIdentifier = symbol.Value;
            
            if (_accountStorage != null)
            {
                var tickerMapping = await _accountStorage.GetTickerCik(symbol.Value);
                if (FSharpOption<TickerCikMapping>.get_IsSome(tickerMapping))
                {
                    // Use CIK for more reliable lookup
                    searchIdentifier = tickerMapping.Value.Cik;
                    _logger?.LogDebug("Resolved ticker {ticker} to CIK {cik}", symbol.Value, searchIdentifier);
                }
                else
                {
                    _logger?.LogWarning("No CIK mapping found for ticker {ticker}, attempting direct lookup", symbol.Value);
                }
            }
            
            // Rate limiting: ensure no more than 2 requests per second
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minimumRequestInterval)
                {
                    var delay = _minimumRequestInterval - timeSinceLastRequest;
                    _logger?.LogDebug("Rate limiting SEC Edgar request for {symbol}, delaying {ms}ms", symbol, delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            var results = await EdgarSearch.CreateAsync(
                stock_symbol: searchIdentifier,
                results_per_page: EdgarSearchResultsPerPage.Entries10
            );

            var filings = new List<CompanyFiling>();

            foreach(var r in results.Results)
            {
                var filing = new CompanyFiling(r.Description,
                    r.DocumentsUrl,
                    r.FilingDate,
                    r.Filing,
                    r.InteractiveDataUrl
                );

                filings.Add(filing);
            }

            var result = new CompanyFilings(symbol, filings);

            return FSharpResult<CompanyFilings, ServiceError>.NewOk(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting SEC filings for {symbol}", symbol);

            return FSharpResult<CompanyFilings, ServiceError>.NewError(
                new ServiceError(ex.Message)
            );
        }
    }
    
    /// <summary>
    /// Fetches the company_tickers.json file from SEC with proper rate limiting and User-Agent.
    /// Returns a dictionary mapping ticker symbol to CompanyTickerEntry.
    /// </summary>
    public async Task<Dictionary<string, CompanyTickerEntry>> FetchCompanyTickers()
    {
        try
        {
            // Rate limiting
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minimumRequestInterval)
                {
                    var delay = _minimumRequestInterval - timeSinceLastRequest;
                    _logger?.LogDebug("Rate limiting SEC API request, delaying {ms}ms", delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            const string url = "https://www.sec.gov/files/company_tickers.json";
            _logger?.LogInformation("Fetching company tickers from {url}", url);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            
            // The JSON structure is: { "0": {...}, "1": {...}, ... }
            using var doc = JsonDocument.Parse(json);
            var result = new Dictionary<string, CompanyTickerEntry>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var element in doc.RootElement.EnumerateObject())
            {
                var entry = new CompanyTickerEntry
                {
                    cik_str = element.Value.GetProperty("cik_str").GetInt32().ToString(),
                    ticker = element.Value.GetProperty("ticker").GetString() ?? "",
                    title = element.Value.GetProperty("title").GetString() ?? ""
                };
                
                if (!string.IsNullOrEmpty(entry.ticker))
                {
                    result[entry.ticker] = entry;
                }
            }
            
            _logger?.LogInformation("Successfully fetched {count} company ticker mappings", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching company tickers from SEC");
            throw;
        }
    }
}
