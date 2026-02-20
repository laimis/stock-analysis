module core.fs.Alerts.SECFilingsMonitoring

open System
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.Shared
open core.fs.Adapters.Brokerage

[<CLIMutable>]
type SECFilingEmailData = 
    {
        ticker: string
        filings: SECFilingForEmail array
    }

and [<CLIMutable>] SECFilingForEmail =
    {
        description: string
        documentUrl: string
        filingDate: string
        reportDate: string
        filing: string
        filingUrl: string
    }

[<CLIMutable>]
type UserFilingsEmailData =
    {
        userName: string
        tickerFilings: SECFilingEmailData array
    }

type SECFilingsMonitoringService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    secFilings: ISECFilings,
    secFilingStorage: ISECFilingStorage,
    emails: IEmailService,
    marketHours: IMarketHours,
    logger: ILogger) =

    let isRecentFiling (filing: CompanyFiling) =
        let today = marketHours.ToMarketTime DateTimeOffset.UtcNow
        today.Date |> DateOnly.FromDateTime |> filing.IsRecentFor

    let ignoreFiling (filing: CompanyFiling) =
        // ignore form 4 filings, they are just so numerous, I would be tracking them all day
        filing.Filing = "4"

    let getUserTickers (userId: UserId) = task {
        let! stocks = portfolio.GetStockPositions userId
        let stockTickers = 
            stocks 
            |> Seq.filter _.IsOpen 
            |> Seq.map _.Ticker

        let! options = portfolio.GetOptionPositions userId
        let optionTickers = 
            options 
            |> Seq.filter _.IsOpen 
            |> Seq.map _.UnderlyingTicker

        return 
            Seq.append stockTickers optionTickers
            |> Seq.distinct
            |> Seq.toArray
    }

    // Concern 1: fetch filings from SEC for a ticker and sync to DB (idempotent, no user context)
    let fetchAndSaveFilings (ticker: Ticker) = async {
        try
            let! response = secFilings.GetFilings ticker |> Async.AwaitTask
            match response with
            | Error err ->
                logger.LogError $"Failed to get SEC filings for {ticker}: {err.Message}"
            | Ok companyFilings ->
                if companyFilings.Filings.Length > 0 then
                    let! cikMapping = accounts.GetTickerCik(ticker.Value) |> Async.AwaitTask
                    let cik =
                        match cikMapping with
                        | Some mapping -> mapping.Cik
                        | None -> "unknown"
                    let filingRecords =
                        companyFilings.Filings
                        |> Seq.map (SECFilingRecord.fromCompanyFiling ticker cik)
                        |> Seq.toArray
                    let! saved = secFilingStorage.SaveFilings filingRecords |> Async.AwaitTask
                    logger.LogInformation $"Synced {ticker}: {saved} new, {companyFilings.Filings.Length} total"
        with ex ->
            logger.LogError $"Error syncing SEC filings for {ticker}: {ex.Message}"
    }

    // Concern 2: query DB for filings since the user's watermark and apply notification filters
    let getNewFilingsForUser (userId: string) (ticker: Ticker) = async {
        try
            let twoDaysAgo = DateTimeOffset.UtcNow.AddDays -2.0
            let! watermark = secFilingStorage.GetWatermark userId ticker |> Async.AwaitTask
            let since = watermark |> Option.defaultValue twoDaysAgo

            let! dbFilings = secFilingStorage.GetFilingsSince ticker since |> Async.AwaitTask

            let filteredFilings =
                dbFilings
                |> Seq.map SECFilingRecord.toCompanyFiling
                |> Seq.filter (fun f -> isRecentFiling f && not (ignoreFiling f))
                |> Seq.toArray

            if filteredFilings.Length > 0 then
                logger.LogInformation $"Found {filteredFilings.Length} new filing(s) for {ticker}"
                return Some (ticker, filteredFilings)
            else
                return None
        with ex ->
            logger.LogError $"Error querying new filings for {ticker}: {ex.Message}"
            return None
    }

    let toEmailData (ticker: Ticker) (filings: CompanyFiling seq) =
        let emailFilings = 
            filings
            |> Seq.map (fun f -> 
                {
                    description = f.Description
                    documentUrl = f.DocumentUrl
                    filingDate = f.FilingDate
                    reportDate = f.ReportDate |> Option.defaultValue ""
                    filing = f.Filing
                    filingUrl = f.FilingUrl
                } : SECFilingForEmail)
            |> Seq.toArray

        {
            ticker = ticker.Value
            filings = emailFilings
        } : SECFilingEmailData

    let processUserFilings (user: User) = async {
        logger.LogInformation $"Checking SEC filings for user {user.State.Email}"

        let! tickers = getUserTickers (UserId user.State.Id) |> Async.AwaitTask
        
        match tickers with
        | [||] ->
            logger.LogInformation $"No tickers to check for user {user.State.Email}"
            return None
        | _ ->
            logger.LogInformation $"Checking {tickers.Length} tickers for user {user.State.Email}"

            // Step 1: sync all tickers to DB (no user context, idempotent)
            do! tickers |> Seq.map fetchAndSaveFilings |> Async.Sequential |> Async.Ignore

            // Step 2: check what's new for this user since their watermark
            let! results =
                tickers
                |> Seq.map (getNewFilingsForUser (user.State.Id.ToString()))
                |> Async.Sequential

            let tickerFilings = results |> Array.choose id

            match tickerFilings with
            | [||] ->
                logger.LogInformation $"No recent filings found for user {user.State.Email}"
                return None
            | _ ->
                logger.LogInformation $"Found filings for {tickerFilings.Length} ticker(s) for user {user.State.Email}"
                return Some (user, tickerFilings)
    }

    let sendEmailToUser (user: User) (tickerFilings: (Ticker * CompanyFiling[]) array) = task {
        let emailTickerFilings = tickerFilings |> Array.map (fun (ticker, filings) -> toEmailData ticker filings)

        let emailData = 
            {
                userName = if String.IsNullOrEmpty(user.State.Firstname) then user.State.Email else user.State.Firstname
                tickerFilings = emailTickerFilings
            }

        let recipient = Recipient(user.State.Email, user.State.Firstname)
        let sender = Sender("support@nightingaletrading.com", "Nightingale Trading")

        let totalFilings = emailTickerFilings |> Array.sumBy (fun tf -> tf.filings.Length)
        let subject = 
            if emailTickerFilings.Length = 1 then
                $"SEC Filing for {emailTickerFilings[0].ticker}"
            else
                $"SEC Filings for {emailTickerFilings.Length} Stocks ({totalFilings} total)"

        try
            let! result = emails.SendSECFilings recipient sender subject emailData
            match result with
            | Ok _ -> 
                logger.LogInformation($"SEC filings email sent successfully to {user.State.Email}")
                return true
            | Error err -> 
                logger.LogError($"Failed to send SEC filings email to {user.State.Email}: {err}")
                return false
        with ex ->
            logger.LogError($"Exception sending SEC filings email to {user.State.Email}: {ex.Message}")
            return false
    }

    let updateWatermarks (userId: string) (tickerFilings: (Ticker * CompanyFiling[]) array) = task {
        let now = DateTimeOffset.UtcNow
        for ticker, _ in tickerFilings do
            do! secFilingStorage.UpsertWatermark userId ticker now
    }

    let executeInternal() = async {
        try
            logger.LogInformation "Starting SEC filings monitoring service"

            let! userPairs = accounts.GetUserEmailIdPairs() |> Async.AwaitTask
            let! users = 
                userPairs 
                |> Seq.map (fun pair -> async { return! accounts.GetUser pair.Id |> Async.AwaitTask })
                |> Async.Sequential
            
            let verifiedUsers = 
                users 
                |> Array.choose id 
                |> Array.filter (fun u -> u.State.Verified.HasValue) 

            logger.LogInformation $"Processing {verifiedUsers.Length} verified users"

            let! results =
                verifiedUsers
                |> Seq.map processUserFilings
                |> Async.Sequential

            let usersWithFilings = results |> Array.choose id

            logger.LogInformation $"Found filings for {usersWithFilings.Length} user(s)"

            for user, tickerFilings in usersWithFilings do
                let! emailSent = sendEmailToUser user tickerFilings |> Async.AwaitTask
                if emailSent then
                    do! updateWatermarks (user.State.Id.ToString()) tickerFilings |> Async.AwaitTask

            logger.LogInformation "SEC filings monitoring service completed successfully"
        with ex ->
            logger.LogError $"Error in SEC filings monitoring service: {ex.Message}"
    }

    interface IApplicationService

    member _.Execute() = task {
        do! executeInternal()
    }
