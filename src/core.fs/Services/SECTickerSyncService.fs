module core.fs.Services.SECTickerSyncService

open System
open System.Linq
open System.Threading.Tasks
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Adapters.SEC

type SECTickerSyncService(
    accountStorage: IAccountStorage,
    secFilingsClient: ISECFilings,
    logger: ILogger) =
    
    member _.Execute() = task {
        try
            logger.LogInformation("Starting SEC ticker-to-CIK mapping sync")
            
            // Check when we last updated
            let! lastUpdated = accountStorage.GetTickerCikLastUpdated()
            
            let shouldUpdate = 
                match lastUpdated with
                | Some lastUpdate ->
                    let daysSinceUpdate = (DateTimeOffset.UtcNow - lastUpdate).TotalDays
                    if daysSinceUpdate >= 7.0 then
                        logger.LogInformation($"Last ticker sync was {daysSinceUpdate:F1} days ago, updating...")
                        true
                    else
                        logger.LogInformation($"Ticker data was updated {daysSinceUpdate:F1} days ago, skipping sync")
                        false
                | None ->
                    logger.LogInformation("No previous ticker sync found, performing initial sync")
                    true
            
            if shouldUpdate then
                // Fetch latest company tickers from SEC
                let! companyTickers = secFilingsClient.FetchCompanyTickers()
                
                logger.LogInformation($"Fetched {companyTickers.Count} company tickers from SEC")
                
                // Convert to TickerCikMapping format
                let now = DateTimeOffset.UtcNow
                let mappings = 
                    companyTickers.Values
                    |> Seq.map (fun (entry: CompanyTickerEntry) ->
                        {
                            Ticker = entry.ticker.ToUpperInvariant()
                            Cik = entry.cik_str.PadLeft(10, '0') // Pad CIK to 10 digits with leading zeros
                            Title = entry.title
                            LastUpdated = now
                        })
                    |> Seq.toArray
                
                // Save to database
                do! accountStorage.SaveTickerCikMappings(mappings)
                
                logger.LogInformation($"Successfully synced {mappings.Length} ticker-to-CIK mappings")
            
        with ex ->
            logger.LogError($"Error syncing SEC ticker mappings: {ex.Message}")
            logger.LogError(ex.ToString())
    }
