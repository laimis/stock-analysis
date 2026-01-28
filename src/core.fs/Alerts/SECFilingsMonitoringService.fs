module core.fs.Alerts.SECFilingsMonitoring

open System
open System.Collections.Generic
open System.Linq
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.fs.Services
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
    emails: IEmailService,
    marketHours: IMarketHours,
    logger: ILogger) =

    let isRecentFiling (filing: CompanyFiling) =
        let today = marketHours.ToMarketTime DateTimeOffset.UtcNow
        today.Date |> DateOnly.FromDateTime |> filing.IsRecentFor

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

    let getFilingsForTicker (ticker: Ticker) = async {
        try
            let! response = secFilings.GetFilings ticker |> Async.AwaitTask
            
            match response with
            | Error err ->
                logger.LogError($"Failed to get SEC filings for {ticker}: {err.Message}")
                return None
            | Ok companyFilings ->
                let recentFilings = 
                    companyFilings.Filings
                    |> Seq.filter isRecentFiling
                    |> Seq.toArray

                if recentFilings.Length > 0 then
                    logger.LogInformation($"Found {recentFilings.Length} recent filing(s) for {ticker}")
                    return Some (ticker, recentFilings)
                else
                    return None
        with ex ->
            logger.LogError($"Error checking SEC filings for {ticker}: {ex.Message}")
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
                    reportDate = f.ReportDate
                    filing = f.Filing
                    filingUrl = f.FilingUrl
                })
            |> Seq.toArray

        {
            ticker = ticker.Value
            filings = emailFilings
        }

    let processUserFilings (user: User) = task {
        logger.LogInformation($"Checking SEC filings for user {user.State.Email}")

        let! tickers = getUserTickers (UserId user.State.Id)
        
        if tickers.Length = 0 then
            logger.LogInformation($"No tickers to check for user {user.State.Email}")
            return None
        else
            logger.LogInformation($"Checking {tickers.Length} tickers for user {user.State.Email}")

            // Check filings for all tickers with rate limiting handled by EdgarClient
            let! results = 
                tickers
                |> Seq.map getFilingsForTicker
                |> Async.Sequential

            let tickerFilings = 
                results
                |> Seq.choose id
                |> Seq.map (fun (ticker, filings) -> toEmailData ticker filings)
                |> Seq.toArray

            if tickerFilings.Length > 0 then
                logger.LogInformation($"Found filings for {tickerFilings.Length} ticker(s) for user {user.State.Email}")
                return Some (user, tickerFilings)
            else
                logger.LogInformation($"No recent filings found for user {user.State.Email}")
                return None
    }

    let sendEmailToUser (user: User) (tickerFilings: SECFilingEmailData array) = task {
        let emailData = 
            {
                userName = if String.IsNullOrEmpty(user.State.Firstname) then user.State.Email else user.State.Firstname
                tickerFilings = tickerFilings
            }

        let recipient = Recipient(user.State.Email, user.State.Firstname)
        let sender = Sender("support@nightingaletrading.com", "Nightingale Trading")

        let totalFilings = tickerFilings |> Array.sumBy (fun tf -> tf.filings.Length)
        let subject = 
            if tickerFilings.Length = 1 then
                $"SEC Filing for {tickerFilings[0].ticker}"
            else
                $"SEC Filings for {tickerFilings.Length} Stocks ({totalFilings} total)"

        try
            let! result = emails.SendSECFilings recipient sender subject emailData
            match result with
            | Ok _ -> 
                logger.LogInformation($"SEC filings email sent successfully to {user.State.Email}")
            | Error err -> 
                logger.LogError($"Failed to send SEC filings email to {user.State.Email}: {err}")
        with ex ->
            logger.LogError($"Exception sending SEC filings email to {user.State.Email}: {ex.Message}")
    }

    interface IApplicationService

    member _.Execute() = task {
        try
            logger.LogInformation("Starting SEC filings monitoring service")

            let! userPairs = accounts.GetUserEmailIdPairs()
            let! users = 
                userPairs 
                |> Seq.map (fun pair -> accounts.GetUser pair.Id)
                |> System.Threading.Tasks.Task.WhenAll
            
            let verifiedUsers = 
                users 
                |> Seq.choose id 
                |> Seq.filter (fun u -> u.State.Verified.HasValue) 
                |> Seq.toArray

            logger.LogInformation($"Processing {verifiedUsers.Length} verified users")

            let! results =
                verifiedUsers
                |> Seq.map processUserFilings
                |> System.Threading.Tasks.Task.WhenAll

            let usersWithFilings = results |> Seq.choose id |> Seq.toArray

            logger.LogInformation($"Found filings for {usersWithFilings.Length} user(s)")

            // Send emails
            for (user, tickerFilings) in usersWithFilings do
                do! sendEmailToUser user tickerFilings

            logger.LogInformation("SEC filings monitoring service completed successfully")
        with ex ->
            logger.LogError($"Error in SEC filings monitoring service: {ex.Message}")
    }
