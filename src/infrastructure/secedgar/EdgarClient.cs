using core.fs;
using core.fs.Adapters.SEC;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using core.fs.Adapters.Storage;

namespace secedgar;

// SEC Submissions API response models
public class SECRecentFilings
{
    [JsonPropertyName("accessionNumber")]
    public string[] AccessionNumber { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("filingDate")]
    public string[] FilingDate { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("reportDate")]
    public string[] ReportDate { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("acceptanceDateTime")]
    public string[] AcceptanceDateTime { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("form")]
    public string[] Form { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("primaryDocument")]
    public string[] PrimaryDocument { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("primaryDocDescription")]
    public string[] PrimaryDocDescription { get; set; } = Array.Empty<string>();
}

public class SECFilingsContainer
{
    [JsonPropertyName("recent")]
    public SECRecentFilings Recent { get; set; } = new();
}

public class SECSubmissionsResponse
{
    [JsonPropertyName("cik")]
    public string Cik { get; set; } = string.Empty;
    
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("tickers")]
    public string[] Tickers { get; set; } = Array.Empty<string>();
    
    [JsonPropertyName("filings")]
    public SECFilingsContainer Filings { get; set; } = new();
}

public class EdgarClient : ISECFilings
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
        
        ConfigureHttpClient();
    }
    
    public EdgarClient(ILogger<EdgarClient>? logger, IAccountStorage? accountStorage, string appName, string appVersion, string email)
    {
        _logger = logger;
        _accountStorage = accountStorage;
        _appName = appName;
        _appVersion = appVersion;
        _email = email;
        
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
            // Resolve ticker to CIK
            string? cik = null;
            
            if (_accountStorage != null)
            {
                var tickerMapping = await _accountStorage.GetTickerCik(symbol.Value);
                if (FSharpOption<TickerCikMapping>.get_IsSome(tickerMapping))
                {
                    cik = tickerMapping.Value.Cik;
                    _logger?.LogDebug("Resolved ticker {ticker} to CIK {cik}", symbol.Value, cik);
                }
                else
                {
                    _logger?.LogWarning("No CIK mapping found for ticker {ticker}", symbol.Value);
                    return FSharpResult<CompanyFilings, ServiceError>.NewError(
                        new ServiceError($"No CIK mapping found for ticker {symbol.Value}. Please sync ticker data first.")
                    );
                }
            }
            else
            {
                _logger?.LogError("Account storage not available for CIK lookup");
                return FSharpResult<CompanyFilings, ServiceError>.NewError(
                    new ServiceError("Account storage not configured")
                );
            }
            
            // Rate limiting: ensure no more than 2 requests per second
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minimumRequestInterval)
                {
                    var delay = _minimumRequestInterval - timeSinceLastRequest;
                    _logger?.LogDebug("Rate limiting SEC API request for {symbol}, delaying {ms}ms", symbol, delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }

            // Fetch from SEC Submissions API
            var url = $"https://data.sec.gov/submissions/CIK{cik}.json";
            _logger?.LogInformation("Fetching SEC filings from {url}", url);
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var submissions = JsonSerializer.Deserialize<SECSubmissionsResponse>(json);
            
            if (submissions?.Filings?.Recent == null)
            {
                _logger?.LogWarning("No filings data returned for {ticker} (CIK {cik})", symbol.Value, cik);
                return FSharpResult<CompanyFilings, ServiceError>.NewOk(
                    new CompanyFilings(symbol, Array.Empty<CompanyFiling>())
                );
            }

            var recent = submissions.Filings.Recent;
            var filings = new List<CompanyFiling>();

            // Parse parallel arrays - all arrays have the same length
            for (int i = 0; i < recent.AccessionNumber.Length && i < 100; i++) // Limit to 100 most recent
            {
                try
                {
                    var filingDate = DateTime.Parse(recent.FilingDate[i]);
                    var acceptanceDate = !string.IsNullOrEmpty(recent.AcceptanceDateTime[i]) 
                        ? DateTime.Parse(recent.AcceptanceDateTime[i]) 
                        : filingDate;
                    
                    // Build URLs using SEC's standard structure
                    var accessionNumber = recent.AccessionNumber[i]; // e.g., "0002107261-26-000002"
                    var accessionNumberNoHyphens = accessionNumber.Replace("-", ""); // e.g., "000210726126000002"
                    var primaryDoc = recent.PrimaryDocument[i];
                    var cikNumber = cik.TrimStart('0'); // Remove leading zeros for URLs
                    
                    // Filing URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{accessionNumber}-index.html
                    var filingUrl = $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{accessionNumber}-index.html";
                    
                    // Document URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{primaryDocument}
                    var documentUrl = $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{primaryDoc}";

                    var filing = new CompanyFiling
                    {
                        Description = recent.PrimaryDocDescription[i],
                        DocumentUrl = documentUrl,
                        FilingDate = filingDate,
                        Filing = recent.Form[i],
                        FilingUrl = filingUrl
                    };

                    filings.Add(filing);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error parsing filing at index {index} for {ticker}", i, symbol.Value);
                    // Continue processing other filings
                }
            }

            _logger?.LogInformation("Retrieved {count} filings for {ticker}", filings.Count, symbol.Value);
            var result = new CompanyFilings(symbol, filings);

            return FSharpResult<CompanyFilings, ServiceError>.NewOk(result);
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error getting SEC filings for {symbol}: {message}", symbol.Value, ex.Message);
            return FSharpResult<CompanyFilings, ServiceError>.NewError(
                new ServiceError($"Failed to fetch filings from SEC: {ex.Message}")
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting SEC filings for {symbol}", symbol.Value);
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
