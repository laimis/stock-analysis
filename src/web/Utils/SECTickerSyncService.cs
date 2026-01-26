using System;
using System.Linq;
using System.Threading.Tasks;
using core.fs.Adapters.Logging;
using core.fs.Adapters.Storage;
using Microsoft.FSharp.Core;
using secedgar;

namespace web.Utils;

public class SECTickerSyncService
{
    private readonly IAccountStorage _accountStorage;
    private readonly EdgarClient _edgarClient;
    private readonly ILogger _logger;

    public SECTickerSyncService(
        IAccountStorage accountStorage,
        EdgarClient edgarClient,
        ILogger logger)
    {
        _accountStorage = accountStorage;
        _edgarClient = edgarClient;
        _logger = logger;
    }

    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting SEC ticker-to-CIK mapping sync");

            // Check when we last updated
            var lastUpdatedOpt = await _accountStorage.GetTickerCikLastUpdated();
            
            bool shouldUpdate;
            if (FSharpOption<DateTimeOffset>.get_IsSome(lastUpdatedOpt))
            {
                var lastUpdate = lastUpdatedOpt.Value;
                var daysSinceUpdate = (DateTimeOffset.UtcNow - lastUpdate).TotalDays;
                
                if (daysSinceUpdate >= 7.0)
                {
                    _logger.LogInformation($"Last ticker sync was {daysSinceUpdate:F1} days ago, updating...");
                    shouldUpdate = true;
                }
                else
                {
                    _logger.LogInformation($"Ticker data was updated {daysSinceUpdate:F1} days ago, skipping sync");
                    shouldUpdate = false;
                }
            }
            else
            {
                _logger.LogInformation("No previous ticker sync found, performing initial sync");
                shouldUpdate = true;
            }

            if (shouldUpdate)
            {
                // Fetch latest company tickers from SEC
                var companyTickers = await _edgarClient.FetchCompanyTickers();
                
                _logger.LogInformation($"Fetched {companyTickers.Count} company tickers from SEC");

                // Convert to TickerCikMapping format
                var now = DateTimeOffset.UtcNow;
                var mappings = companyTickers.Values
                    .Select(entry => new TickerCikMapping
                    {
                        Ticker = entry.ticker.ToUpperInvariant(),
                        Cik = entry.cik_str.PadLeft(10, '0'), // Pad CIK to 10 digits with leading zeros
                        Title = entry.title,
                        LastUpdated = now
                    })
                    .ToArray();

                // Save to database
                await _accountStorage.SaveTickerCikMappings(mappings);

                _logger.LogInformation($"Successfully synced {mappings.Length} ticker-to-CIK mappings");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error syncing SEC ticker mappings: {ex.Message}");
            _logger.LogError(ex.ToString());
        }
    }
}
