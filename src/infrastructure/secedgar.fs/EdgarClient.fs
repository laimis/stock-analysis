namespace secedgar.fs

open System
open System.Collections.Generic
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open core.Shared
open core.fs
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage

// SEC Submissions API response models
[<AllowNullLiteral>]
type SECRecentFilings() =
    [<JsonPropertyName("accessionNumber")>]
    member val accessionNumber: string[] = null with get, set
    [<JsonPropertyName("filingDate")>]
    member val filingDate: string[] = null with get, set
    [<JsonPropertyName("reportDate")>]
    member val reportDate: string[] = null with get, set
    [<JsonPropertyName("acceptanceDateTime")>]
    member val acceptanceDateTime: string[] = null with get, set
    [<JsonPropertyName("form")>]
    member val form: string[] = null with get, set
    [<JsonPropertyName("primaryDocument")>]
    member val primaryDocument: string[] = null with get, set
    [<JsonPropertyName("primaryDocDescription")>]
    member val primaryDocDescription: string[] = null with get, set

[<AllowNullLiteral>]
type SECFilingsContainer() =
    [<JsonPropertyName("recent")>]
    member val recent: SECRecentFilings = null with get, set

[<AllowNullLiteral>]
type SECSubmissionsResponse() =
    [<JsonPropertyName("cik")>]
    member val cik: string = null with get, set
    [<JsonPropertyName("entityType")>]
    member val entityType: string = null with get, set
    [<JsonPropertyName("name")>]
    member val name: string = null with get, set
    [<JsonPropertyName("tickers")>]
    member val tickers: string[] = null with get, set
    [<JsonPropertyName("filings")>]
    member val filings: SECFilingsContainer = null with get, set

type EdgarClient(logger: ILogger<EdgarClient> option, accountStorage: IAccountStorage option, appName: string, appVersion: string, email: string) =
    
    static let rateLimitSemaphore = new SemaphoreSlim(1, 1)
    static let mutable lastRequestTime = DateTime.MinValue
    static let minimumRequestInterval = TimeSpan.FromMilliseconds 200.0

    let rateLimitAsync() = async {
        do! rateLimitSemaphore.WaitAsync() |> Async.AwaitTask
        try
            let timeSinceLastRequest = DateTime.UtcNow - lastRequestTime
            if timeSinceLastRequest < minimumRequestInterval then
                let delay = minimumRequestInterval - timeSinceLastRequest
                logger |> Option.iter (fun l -> l.LogDebug("Rate limiting SEC API request, delaying {ms}ms", delay.TotalMilliseconds))
                do! Task.Delay(delay) |> Async.AwaitTask
            lastRequestTime <- DateTime.UtcNow
        finally
            rateLimitSemaphore.Release() |> ignore
    }

    let resolveCikAsync (ticker: Ticker) = async {
        match accountStorage with
        | None ->
            logger |> Option.iter (fun l -> l.LogError("Account storage not available for CIK lookup"))
            return Error (ServiceError("Account storage not configured"))
        | Some storage ->
            let! tickerMapping = storage.GetTickerCik(ticker.Value) |> Async.AwaitTask
            match tickerMapping with
            | Some mapping ->
                logger |> Option.iter (fun l -> l.LogDebug("Resolved ticker {ticker} to CIK {cik}", ticker.Value, mapping.Cik))
                return Ok mapping.Cik
            | None ->
                logger |> Option.iter (fun l -> l.LogWarning("No CIK mapping found for ticker {ticker}", ticker.Value))
                return Error (ServiceError($"No CIK mapping found for ticker {ticker.Value}. Please sync ticker data first."))
    }

    let httpClient = new HttpClient()
    
    do
        // Configure HttpClient with proper User-Agent for SEC API calls
        httpClient.DefaultRequestHeaders.Clear()
        httpClient.DefaultRequestHeaders.Add("User-Agent", $"{appName}/{appVersion} ({email})")
    
    // Convenience constructors
    new(logger: ILogger<EdgarClient> option) = 
        EdgarClient(logger, None, "NGTDTrading", "1.0", "secclient@nightingaletrading.com")
    
    new(logger: ILogger<EdgarClient> option, accountStorage: IAccountStorage option) = 
        EdgarClient(logger, accountStorage, "NGTDTrading", "1.0", "secclient@nightingaletrading.com")
    
    new(logger: ILogger<EdgarClient> option, appName: string, appVersion: string, email: string) = 
        EdgarClient(logger, None, appName, appVersion, email)
    
    
    /// Parse a single filing from parallel arrays
    member private _.ParseFiling(recent: SECRecentFilings, index: int, cik: string, ticker: Ticker) =
        try
            let filingDate = DateTime.ParseExact(recent.filingDate.[index], "yyyy-MM-dd", null)
            let reportDate = 
                if String.IsNullOrEmpty(recent.reportDate.[index]) then filingDate
                else DateTime.ParseExact(recent.reportDate.[index], "yyyy-MM-dd", null)
            
            // Build URLs using SEC's standard structure
            let accessionNumber = recent.accessionNumber.[index] // e.g., "0002107261-26-000002"
            let accessionNumberNoHyphens = accessionNumber.Replace("-", "") // e.g., "000210726126000002"
            let cikNumber = cik.TrimStart('0') // Remove leading zeros for URLs
            
            // Filing URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{accessionNumber}-index.html
            let filingUrl = $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{accessionNumber}-index.html"
            
            // Document URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{primaryDocument}
            let documentUrl = 
                if recent.primaryDocument <> null 
                   && index < recent.primaryDocument.Length 
                   && not (String.IsNullOrWhiteSpace(recent.primaryDocument.[index])) then
                    let primaryDoc = recent.primaryDocument.[index]
                    $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{primaryDoc}"
                else
                    filingUrl
            
            let filing = {
                Description = recent.primaryDocDescription.[index]
                DocumentUrl = documentUrl
                FilingDate = filingDate
                ReportDate = reportDate
                Filing = recent.form.[index]
                FilingUrl = filingUrl
            }
            
            Some filing
        with ex ->
            logger |> Option.iter (fun l -> l.LogWarning(ex, "Error parsing filing at index {index} for {ticker}", index, ticker.Value))
            None
    
    interface ISECFilings with
        member this.GetFilings(ticker: Ticker) = 
            async {
                try
                    // Resolve ticker to CIK
                    let! cikResult = resolveCikAsync ticker
                    match cikResult with
                    | Error err -> return Error err
                    | Ok cik ->
                        do! rateLimitAsync()
                        
                        // Fetch from SEC Submissions API
                        let url = $"https://data.sec.gov/submissions/CIK{cik}.json"
                        logger |> Option.iter (fun l -> l.LogInformation("Fetching SEC filings from {url}", url))
                        
                        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                        response.EnsureSuccessStatusCode() |> ignore
                        
                        let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                        let submissions = JsonSerializer.Deserialize<SECSubmissionsResponse>(json)
                        
                        if isNull submissions || isNull submissions.filings || isNull submissions.filings.recent then
                            logger |> Option.iter (fun l -> l.LogWarning("No filings data returned for {ticker} (CIK {cik})", ticker.Value, cik))
                            return Ok (CompanyFilings(ticker, [||]))
                        else
                            let recent = submissions.filings.recent
                            let maxFilings = min recent.accessionNumber.Length 100 // Limit to 100 most recent
                            
                            let filings = 
                                [| 0 .. maxFilings - 1 |]
                                |> Array.choose (fun i -> this.ParseFiling(recent, i, cik, ticker))
                            
                            logger |> Option.iter (fun l -> l.LogInformation("Retrieved {count} filings for {ticker}", filings.Length, ticker.Value))
                            return Ok (CompanyFilings(ticker, filings))
                
                with 
                | :? HttpRequestException as ex ->
                    logger |> Option.iter (fun l -> l.LogError(ex, "HTTP error getting SEC filings for {symbol}: {message}", ticker.Value, ex.Message))
                    return Error (ServiceError($"Failed to fetch filings from SEC: {ex.Message}"))
                | ex ->
                    logger |> Option.iter (fun l -> l.LogError(ex, "Error getting SEC filings for {symbol}", ticker.Value))
                    return Error (ServiceError(ex.Message))
            }
            |> Async.StartAsTask
        
        /// Fetches the company_tickers.json file from SEC with proper rate limiting and User-Agent.
        /// Returns a dictionary mapping ticker symbol to CompanyTickerEntry.
        member this.FetchCompanyTickers() = 
            async {
                try
                    // Rate limiting
                    do! rateLimitAsync()
                    
                    let url = "https://www.sec.gov/files/company_tickers.json"
                    logger |> Option.iter (fun l -> l.LogInformation("Fetching company tickers from {url}", url))
                    
                    let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                    response.EnsureSuccessStatusCode() |> ignore
                    
                    let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    
                    // The JSON structure is: { "0": {...}, "1": {...}, ... }
                    use doc = JsonDocument.Parse(json)
                    let result = Dictionary<string, CompanyTickerEntry>(StringComparer.OrdinalIgnoreCase)
                    
                    for element in doc.RootElement.EnumerateObject() do
                        let entry = {
                            cik_str = element.Value.GetProperty("cik_str").GetInt32().ToString()
                            ticker = 
                                let tickerProp = element.Value.GetProperty("ticker")
                                if tickerProp.ValueKind = JsonValueKind.String then tickerProp.GetString() else ""
                            title = 
                                let titleProp = element.Value.GetProperty("title")
                                if titleProp.ValueKind = JsonValueKind.String then titleProp.GetString() else ""
                        }
                        
                        if not (String.IsNullOrEmpty(entry.ticker)) then
                            result.[entry.ticker] <- entry
                    
                    logger |> Option.iter (fun l -> l.LogInformation("Successfully fetched {count} company ticker mappings", result.Count))
                    return result
                
                with ex ->
                    logger |> Option.iter (fun l -> l.LogError(ex, "Error fetching company tickers from SEC"))
                    return raise ex
            }
            |> Async.StartAsTask
