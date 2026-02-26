namespace secedgar.fs

open System
open System.Collections.Generic
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.RegularExpressions
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
    [<JsonPropertyName("isXBRL")>]
    member val isXBRL: int[] = null with get, set
    [<JsonPropertyName("isInlineXBRL")>]
    member val isInlineXBRL: int[] = null with get, set

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
                logger |> Option.iter (fun l -> l.LogDebug("No CIK mapping found for ticker {ticker}, skipping SEC filings sync", ticker.Value))
                return Error (ServiceError $"No CIK mapping found for ticker {ticker.Value}. Please sync ticker data first.")
    }

    let parseFiling(recent: SECRecentFilings) (index: int) (cik: string) (ticker: Ticker) =
        try
            let filingDate = recent.filingDate[index]
            let reportDate = recent.reportDate[index]
            
            // Build URLs using SEC's standard structure
            let accessionNumber = recent.accessionNumber[index] // e.g., "0002107261-26-000002"
            let accessionNumberNoHyphens = accessionNumber.Replace("-", "") // e.g., "000210726126000002"
            let cikNumber = cik.TrimStart '0' // Remove leading zeros for URLs
            let isXBRL = recent.isXBRL[index] = 1
            let isInlineXBRL = recent.isInlineXBRL[index] = 1
            
            // Filing URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{accessionNumber}-index.html
            let filingUrl = $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{accessionNumber}-index.html"
            
            // Document URL: https://www.sec.gov/Archives/edgar/data/{cik}/{accessionNumberNoHyphens}/{primaryDocument}
            let documentUrl = 
                if recent.primaryDocument <> null 
                   && index < recent.primaryDocument.Length 
                   && not (String.IsNullOrWhiteSpace(recent.primaryDocument[index])) then
                    let primaryDoc = recent.primaryDocument[index]
                    $"https://www.sec.gov/Archives/edgar/data/{cikNumber}/{accessionNumberNoHyphens}/{primaryDoc}"
                else
                    filingUrl
            
            // Get best description (user-friendly fallback if SEC description is poor)
            let filingType = recent.form[index]
            let rawDescription = recent.primaryDocDescription[index]
            let description = FilingDescriptions.getBestDescription rawDescription filingType
            
            let filing = {
                Description = description
                DocumentUrl = documentUrl
                FilingDate = filingDate
                ReportDate = if String.IsNullOrWhiteSpace(reportDate) then None else Some reportDate
                Filing = filingType
                FilingUrl = filingUrl
                IsXBRL = isXBRL
                IsInlineXBRL = isInlineXBRL
            }
            
            Some filing
        with ex ->
            logger |> Option.iter (fun l -> l.LogWarning(ex, "Error parsing filing at index {index} for {ticker}", index, ticker.Value))
            None

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
    
    
    interface ISECFilings with
        member this.GetFilings(ticker: Ticker) = 
            task {
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
                        
                        let! response = httpClient.GetAsync url
                        response.EnsureSuccessStatusCode() |> ignore
                        
                        let! json = response.Content.ReadAsStringAsync()
                        let submissions = JsonSerializer.Deserialize<SECSubmissionsResponse> json
                        
                        if isNull submissions || isNull submissions.filings || isNull submissions.filings.recent then
                            logger |> Option.iter (fun l -> l.LogWarning("No filings data returned for {ticker} (CIK {cik})", ticker.Value, cik))
                            return Ok (CompanyFilings(ticker, [||]))
                        else
                            let recent = submissions.filings.recent
                            let maxFilings = min recent.accessionNumber.Length 100 // Limit to 100 most recent
                            
                            let filings = 
                                [| 0 .. maxFilings - 1 |]
                                |> Array.choose (fun i -> parseFiling recent i cik ticker)
                            
                            logger |> Option.iter (fun l -> l.LogInformation("Retrieved {count} filings for {ticker}", filings.Length, ticker.Value))
                            return Ok (CompanyFilings(ticker, filings))
                
                with 
                | :? HttpRequestException as ex ->
                    logger |> Option.iter (fun l -> l.LogError(ex, "HTTP error getting SEC filings for {symbol}: {message}", ticker.Value, ex.Message))
                    return Error (ServiceError $"Failed to fetch filings from SEC: {ex.Message}")
                | ex ->
                    logger |> Option.iter (fun l -> l.LogError(ex, "Error getting SEC filings for {symbol}", ticker.Value))
                    return Error (ServiceError ex.Message)
            }
        
        /// Fetches the company_tickers.json file from SEC with proper rate limiting and User-Agent.
        /// Returns a dictionary mapping ticker symbol to CompanyTickerEntry.
        member this.FetchCompanyTickers() = 
            task {
                
                // Rate limiting
                do! rateLimitAsync()
                
                let url = "https://www.sec.gov/files/company_tickers.json"
                logger |> Option.iter (fun l -> l.LogInformation("Fetching company tickers from {url}", url))
                
                let! response = httpClient.GetAsync url
                response.EnsureSuccessStatusCode() |> ignore
                
                let! json = response.Content.ReadAsStringAsync()
                
                // The JSON structure is: { "0": {...}, "1": {...}, ... }
                use doc = JsonDocument.Parse json
                let result = Dictionary<string, CompanyTickerEntry> StringComparer.OrdinalIgnoreCase
                
                for element in doc.RootElement.EnumerateObject() do
                    let entry = {
                        cik_str = element.Value.GetProperty("cik_str").GetInt32().ToString()
                        ticker = 
                            let tickerProp = element.Value.GetProperty "ticker"
                            if tickerProp.ValueKind = JsonValueKind.String then tickerProp.GetString() else ""
                        title = 
                            let titleProp = element.Value.GetProperty "title"
                            if titleProp.ValueKind = JsonValueKind.String then titleProp.GetString() else ""
                    }
                    
                    if not (String.IsNullOrEmpty entry.ticker) then
                        result.[entry.ticker] <- entry
                
                logger |> Option.iter (fun l -> l.LogInformation("Successfully fetched {count} company ticker mappings", result.Count))
                return result
            }

        member this.FetchPrimaryDocument (filingUrl: string): Task<Result<string,ServiceError>> = task {
            try
                logger |> Option.iter (fun l -> l.LogDebug("Fetching filing page to find XML URL: {url}", filingUrl))
                
                // Rate limiting
                do! rateLimitAsync()
                
                // Fetch the HTML filing index page
                let! response = httpClient.GetAsync filingUrl
                response.EnsureSuccessStatusCode() |> ignore
                let! html = response.Content.ReadAsStringAsync()
                
                // Look for primary_doc.xml in the HTML table (excluding xsl URLs)
                // The table row looks like: | 1 |   | primary_doc.xml | SCHEDULE 13G/A | 6835 |
                let xmlPattern = @"(primary_doc\.xml)"
                
                let xmlMatch = Regex.Match(html, xmlPattern, RegexOptions.IgnoreCase)
                
                if xmlMatch.Success then
                    // Extract the base directory from the filing URL
                    // https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/0000315066-26-000439-index.html
                    // -> https://www.sec.gov/Archives/edgar/data/1516513/000031506626000439/
                    let baseUrl = 
                        let lastSlashIndex = filingUrl.LastIndexOf('/')
                        if lastSlashIndex >= 0 then
                            filingUrl.Substring(0, lastSlashIndex + 1)
                        else
                            filingUrl + "/"
                    
                    let xmlUrl = baseUrl + "primary_doc.xml"
                    
                    logger |> Option.iter (fun l -> l.LogInformation("Found XML document URL: {xmlUrl}", xmlUrl))
                    
                    // now that we have the XML URL, we can fetch it
                    let! xmlResponse = httpClient.GetAsync xmlUrl
                    xmlResponse.EnsureSuccessStatusCode() |> ignore
                    let! xmlContent = xmlResponse.Content.ReadAsStringAsync()
                    return Ok xmlContent
                else
                    let errorMsg = "Could not find primary_doc.xml in filing page"
                    logger |> Option.iter (fun l -> l.LogWarning("{error} for URL: {url}", errorMsg, filingUrl))
                    return Error (ServiceError errorMsg)
                    
            with
            | :? HttpRequestException as ex ->
                logger |> Option.iter (fun l -> l.LogError(ex, "HTTP error fetching filing page: {url}", filingUrl))
                return Error (ServiceError $"Failed to fetch filing page: {ex.Message}")
            | ex ->
                logger |> Option.iter (fun l -> l.LogError(ex, "Error parsing filing page: {url}", filingUrl))
                return Error (ServiceError $"Error parsing filing page: {ex.Message}")
        } 
            
