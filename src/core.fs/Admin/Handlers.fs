namespace core.fs.Admin

open core.fs
open core.fs.Accounts
open core.fs.Adapters.CSV
open core.fs.Adapters.Email
open core.fs.Adapters.SEC
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Options
open core.fs.Services
open core.fs.Services.SECTickerSyncService
open core.fs.Stocks
open core.Shared

type Query = {
    everyone: bool
}

type TriggerSECTickerSync = struct end
        
type QueryResponse(user:User, stocks:StockPositionState seq, options:OptionPositionState seq) =
    let stockLength = stocks |> Seq.length
    let optionLength = options |> Seq.length
    
    member _.Email = user.State.Email
    member _.Id = user.State.Id
    member _.FirstName = user.State.Firstname
    member _.LastName = user.State.Lastname
    member _.Verified = user.State.Verified.HasValue
    member _.Stocks = stockLength
    member _.Options = optionLength
        
type Export = struct end

type SendWelcomeEmail = {
    userId:UserId
}

type SendEmail = {
    input:EmailInput
}

type MigrateSECFilings = {
    userEmail:string
}

type MigrateSECFilingsResponse = {
    UserEmail: string
    TickersProcessed: int
    TotalFilingsSaved: int
    Details: string list
}
        
type Handler(storage:IAccountStorage, email:IEmailService, portfolio:IPortfolioStorage, csvWriter:ICSVWriter, secTickerSync:SECTickerSyncService, secFilings:ISECFilings, secFilingStorage:ISECFilingStorage, logger:ILogger) =
            
    let buildQueryResponse userId =
        async {
            let! user = storage.GetUser(userId) |> Async.AwaitTask
            let! options = portfolio.GetOptionPositions(userId) |> Async.AwaitTask
            let! stocks = portfolio.GetStockPositions(userId) |> Async.AwaitTask
            
            return QueryResponse(user.Value, stocks, options)
        }
                
    interface IApplicationService
    
    member _.Handle (cmd:SendEmail) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        let! emailResult = cmd.input |> email.SendWithInput
        match emailResult with
        | Ok () -> return Ok ()
        | Error err -> return ServiceError err |> Error
    }
    
    member _.Handle sendWelcome : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        let! user = sendWelcome.userId |> storage.GetUser 
        match user with
        | Some user ->
            let! emailResult = email.SendWelcome (Recipient(email=user.State.Email, name=user.State.Name)) Sender.Support {||}
            match emailResult with
            | Ok () -> return Ok ()
            | Error err -> return ServiceError err |> Error
            
        | None -> return "User not found" |> ServiceError |> Error
        
    }
            
    member _.Handle (_:Query) : System.Threading.Tasks.Task<Result<QueryResponse array,ServiceError>> =
        task {
            let! users = storage.GetUserEmailIdPairs()
            
            let! result = 
                users
                |> Seq.map (fun emailId -> emailId.Id |> buildQueryResponse)
                |> Async.Parallel
                |> Async.StartAsTask
                
            return result |> Ok
        }
        
    member _.Handle (_:Export) = task {
        let! pairs = storage.GetUserEmailIdPairs()
        
        let! userTasks = 
            pairs
            |> Seq.map (fun emailId ->
                emailId.Id |> storage.GetUser |> Async.AwaitTask
            )
            |> Async.Parallel
            |> Async.StartAsTask
            
        let users = userTasks |> Seq.choose id
            
        let filename = CSVExport.generateFilename "users"
        
        return ExportResponse(filename, CSVExport.users csvWriter users)
    }
    
    member _.Handle (_:TriggerSECTickerSync) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
        do! secTickerSync.Execute()
        return Ok ()
    }
    
    member _.Handle (cmd:MigrateSECFilings) : System.Threading.Tasks.Task<Result<MigrateSECFilingsResponse,ServiceError>> = task {
        try
            logger.LogInformation($"Starting SEC filings migration for user: {cmd.userEmail}")
            
            // Get user by email
            let! user = storage.GetUserByEmail(cmd.userEmail)
            match user with
            | None -> 
                logger.LogWarning($"User not found: {cmd.userEmail}")
                return Error (ServiceError $"User not found: {cmd.userEmail}")
            | Some user ->
                logger.LogInformation($"Found user {user.State.Email} (ID: {user.State.Id})")
                
                let userId = UserId(user.State.Id)
                
                // Get user's portfolio positions
                let! stocks = portfolio.GetStockPositions userId
                let! options = portfolio.GetOptionPositions userId
                
                // Extract unique tickers from open positions
                let stockTickers = 
                    stocks 
                    |> Seq.filter (fun s -> s.IsOpen) 
                    |> Seq.map (fun s -> s.Ticker)
                    
                let optionTickers = 
                    options 
                    |> Seq.filter (fun o -> o.IsOpen)
                    |> Seq.map (fun o -> o.UnderlyingTicker)
                
                let allTickers = 
                    Seq.append stockTickers optionTickers
                    |> Seq.distinct
                    |> Seq.toArray
                
                logger.LogInformation($"Found {allTickers.Length} unique tickers in open positions")
                
                if allTickers.Length = 0 then
                    return Ok {
                        UserEmail = cmd.userEmail
                        TickersProcessed = 0
                        TotalFilingsSaved = 0
                        Details = ["No open positions found"]
                    }
                else
                    let mutable totalFilingsSaved = 0
                    let details = System.Collections.Generic.List<string>()
                    
                    // Process each ticker
                    for ticker in allTickers do
                        try
                            logger.LogInformation($"Fetching SEC filings for {ticker.Value}")
                            
                            // Fetch all SEC filings for this ticker
                            let! filingsResult = secFilings.GetFilings ticker
                            
                            match filingsResult with
                            | Error err ->
                                let msg = $"Failed to fetch filings for {ticker.Value}: {err.Message}"
                                logger.LogWarning(msg)
                                details.Add(msg)
                            | Ok companyFilings ->
                                let filings = companyFilings.Filings |> Seq.toArray
                                logger.LogInformation($"Fetched {filings.Length} filings for {ticker.Value}")
                                
                                if filings.Length > 0 then
                                    // Get CIK for this ticker
                                    let! cikMapping = storage.GetTickerCik(ticker.Value)
                                    let cik = 
                                        match cikMapping with
                                        | Some mapping -> mapping.Cik
                                        | None -> "unknown"
                                    
                                    // Convert to storage records
                                    let filingRecords = 
                                        filings
                                        |> Seq.map (SECFilingRecord.fromCompanyFiling ticker cik)
                                        |> Seq.toArray
                                    
                                    // Save filings to database (ignores duplicates); returns only inserted rows
                                    let! savedFilings = secFilingStorage.SaveFilings filingRecords
                                    let savedCount = savedFilings |> Seq.length
                                    totalFilingsSaved <- totalFilingsSaved + savedCount
                                    
                                    let msg = $"{ticker.Value}: Saved {savedCount}/{filings.Length} filings"
                                    logger.LogInformation(msg)
                                    details.Add(msg)
                                else
                                    let msg = $"{ticker.Value}: No filings found"
                                    logger.LogInformation(msg)
                                    details.Add(msg)
                            
                            // Add small delay to respect rate limits
                            do! System.Threading.Tasks.Task.Delay(100)
                            
                        with ex ->
                            let msg = $"Error processing {ticker.Value}: {ex.Message}"
                            logger.LogError(msg)
                            details.Add(msg)
                    
                    logger.LogInformation($"SEC filings migration completed. Processed {allTickers.Length} tickers, saved {totalFilingsSaved} total filings")
                    
                    return Ok {
                        UserEmail = cmd.userEmail
                        TickersProcessed = allTickers.Length
                        TotalFilingsSaved = totalFilingsSaved
                        Details = details |> Seq.toList
                    }
        with ex ->
            logger.LogError($"Error in SEC filings migration: {ex.Message}")
            return Error (ServiceError ex.Message)
    }
