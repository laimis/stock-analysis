using core.fs;
using core.fs.Adapters.SEC;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using SecuritiesExchangeCommission.Edgar;

namespace secedgar;
public class EdgarClient : ISECFilings
{
    private readonly ILogger<EdgarClient>? _logger;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minimumRequestInterval = TimeSpan.FromMilliseconds(500); // Max 2 requests per second

    public EdgarClient(ILogger<EdgarClient>? logger) : this(logger, "TradeWatch", "1.0", "support@tradewatch.io"){}
    

    public EdgarClient(ILogger<EdgarClient>? logger, string appName, string appVersion, string email)
    {
        _logger = logger;
        
        // SEC requires all automated tools to identify themselves via User-Agent
        // This must be set before any EdgarSearch calls
        IdentificationManager.Instance.AppName = appName;
        IdentificationManager.Instance.AppVersion = appVersion;
        IdentificationManager.Instance.Email = email;
    }
        

    public async Task<FSharpResult<CompanyFilings,ServiceError>> GetFilings(Ticker symbol)
    {
        try
        {
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
                stock_symbol: symbol.Value,
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
}
