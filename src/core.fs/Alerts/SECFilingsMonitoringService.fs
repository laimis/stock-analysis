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
        // Ownership enrichment fields – non-empty only for Form 144 and Schedule 13G filings
        ownershipEntityName: string
        ownershipShares: string
        ownershipTotalValue: string
        ownershipPercent: string
        ownershipSummary: string
    }

[<CLIMutable>]
type UserFilingsEmailData =
    {
        userName: string
        tickerFilings: SECFilingEmailData array
    }

/// Shared helper: returns open stock + option tickers for a given user.
let private getUserTickers (portfolio: IPortfolioStorage) (userId: UserId) = task {
    let! stocks = portfolio.GetStockPositions userId
    let stockTickers = stocks |> Seq.filter _.IsOpen |> Seq.map _.Ticker

    let! options = portfolio.GetOptionPositions userId
    let optionTickers = options |> Seq.filter _.IsOpen |> Seq.map _.UnderlyingTicker

    return
        Seq.append stockTickers optionTickers
        |> Seq.distinct
        |> Seq.toArray
}

/// Shared helper: loads verified users.
let private getVerifiedUsers (accounts: IAccountStorage) = async {
    let! userPairs = accounts.GetUserEmailIdPairs() |> Async.AwaitTask
    let! users =
        userPairs
        |> Seq.map (fun pair -> async { return! accounts.GetUser pair.Id |> Async.AwaitTask })
        |> Async.Sequential
    return users |> Array.choose id |> Array.filter (fun u -> u.State.Verified.HasValue)
}

// ─────────────────────────────────────────────────────────────────────────────
// Job 1: sync SEC filings from the SEC API into the database.
// Runs frequently (e.g. every 10 minutes) so the DB stays up to date.
// No user-specific logic, no emails.
// ─────────────────────────────────────────────────────────────────────────────

type SECFilingsSyncService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    secFilings: ISECFilings,
    secFilingStorage: ISECFilingStorage,
    logger: ILogger) =

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

    let executeInternal() = async {
        try
            logger.LogInformation "Starting SEC filings sync service"

            let! verifiedUsers = getVerifiedUsers accounts

            logger.LogInformation $"Collecting tickers from {verifiedUsers.Length} verified user(s)"

            // Collect all tickers across all users, deduplicated, to avoid redundant SEC API calls.
            let! allTickers =
                verifiedUsers
                |> Seq.map (fun u -> async {
                    return! getUserTickers portfolio (UserId u.State.Id) |> Async.AwaitTask
                })
                |> Async.Sequential

            let uniqueTickers =
                allTickers
                |> Seq.concat
                |> Seq.distinctBy _.Value
                |> Seq.toArray

            logger.LogInformation $"Syncing SEC filings for {uniqueTickers.Length} unique ticker(s)"

            do! uniqueTickers |> Seq.map fetchAndSaveFilings |> Async.Sequential |> Async.Ignore

            logger.LogInformation "SEC filings sync service completed successfully"
        with ex ->
            logger.LogError $"Error in SEC filings sync service: {ex.Message}"
    }

    interface IApplicationService

    member _.Execute() = task {
        do! executeInternal()
    }

// ─────────────────────────────────────────────────────────────────────────────
// Job 2: notify users about new filings that have appeared in the DB since
// each user's last watermark. Runs less frequently (e.g. every 30 minutes).
// No SEC API calls – reads only from the DB.
// ─────────────────────────────────────────────────────────────────────────────

type SECFilingsMonitoringService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    secFilingStorage: ISECFilingStorage,
    ownershipStorage: IOwnershipStorage,
    emails: IEmailService,
    marketHours: IMarketHours,
    logger: ILogger) =

    let isRecentFiling (filingDate: string) =
        let today = marketHours.ToMarketTime DateTimeOffset.UtcNow
        today.Date |> DateOnly.FromDateTime |> FilingDate.isRecent <| filingDate

    let ignoreFiling (formType: string) =
        // ignore form 4 filings, they are just so numerous, I would be tracking them all day
        formType = "4"

    let getNewFilingsForUser (userId: string) (ticker: Ticker) = async {
        try
            let twoDaysAgo = DateTimeOffset.UtcNow.AddDays -2.0
            let! watermark = secFilingStorage.GetWatermark userId ticker |> Async.AwaitTask
            let since = watermark |> Option.defaultValue twoDaysAgo

            let! dbFilings = secFilingStorage.GetFilingsSince ticker since |> Async.AwaitTask

            let filteredFilings =
                dbFilings
                |> Seq.filter (fun f -> isRecentFiling f.FilingDate && not (ignoreFiling f.FormType))
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

    let formatShares (shares: int64) = shares.ToString("N0")

    let formatCurrency (value: decimal) =
        if value >= 1_000_000m then $"${value / 1_000_000m:F1}M"
        elif value >= 1_000m then $"${value / 1_000m:F1}K"
        else $"${value:F2}"

    let buildOwnershipDetails (filing: SECFilingRecord) = task {
        try
            let formType = filing.FormType.ToUpperInvariant()
            let isOwnershipForm =
                formType.Contains "13G"
                || formType = "144"
                || formType = "144/A"

            if not isOwnershipForm then
                return "", "", "", "", ""
            else
                let! events = ownershipStorage.GetEventsByFilingId filing.Id
                match events |> Seq.tryHead with
                | None -> return "", "", "", "", ""
                | Some ev ->
                    let! entityOpt = ownershipStorage.GetEntityById ev.EntityId
                    let entityName = entityOpt |> Option.map _.Name |> Option.defaultValue "<Unknown Entity>"

                    if formType = "144" || formType = "144/A" then
                        let shares = ev.SharesTransacted |> Option.map formatShares |> Option.defaultValue ""
                        let value  = ev.TotalValue |> Option.map formatCurrency |> Option.defaultValue ""
                        let summary =
                            match shares, value with
                            | s, v when s <> "" && v <> "" -> $"Proposed sale of {s} shares for {v}"
                            | s, _  when s <> ""           -> $"Proposed sale of {s} shares"
                            | _ -> "Proposed insider sale (Form 144)"
                        return entityName, shares, value, "", summary
                    else
                        // Schedule 13G / 13G-A
                        let shares = ev.SharesAfter |> Option.map formatShares |> Option.defaultValue ""
                        let pct    = ev.PercentOfClass |> Option.map (fun p -> $"{p:F2}%%") |> Option.defaultValue ""
                        let summary =
                            match shares, pct with
                            | s, p when s <> "" && p <> "" -> $"{entityName} reports {s} shares ({p} of class)"
                            | s, _  when s <> ""           -> $"{entityName} reports {s} shares"
                            | _ -> $"{entityName} ownership disclosure"
                        return entityName, shares, "", pct, summary
        with ex ->
            logger.LogError $"Error building ownership details for filing {filing.FilingUrl}: {ex}"
            return "", "", "", "", ""
    }

    let toEmailData (ticker: Ticker) (filings: SECFilingRecord seq) = async {
        let! emailFilings =
            filings
            |> Seq.map (fun f -> async {
                let! entityName, shares, value, pct, summary = buildOwnershipDetails f |> Async.AwaitTask
                return {
                    description = f.Description
                    documentUrl = f.DocumentUrl
                    filingDate = f.FilingDate
                    reportDate = f.ReportDate |> Option.defaultValue ""
                    filing = f.FormType
                    filingUrl = f.FilingUrl
                    ownershipEntityName = entityName
                    ownershipShares = shares
                    ownershipTotalValue = value
                    ownershipPercent = pct
                    ownershipSummary = summary
                } : SECFilingForEmail
            })
            |> Async.Sequential

        return {
            ticker = ticker.Value
            filings = emailFilings
        } : SECFilingEmailData
    }

    let processUserFilings (user: User) = async {
        logger.LogInformation $"Checking SEC filings for user {user.State.Email}"

        let! tickers = getUserTickers portfolio (UserId user.State.Id) |> Async.AwaitTask

        match tickers with
        | [||] ->
            logger.LogInformation $"No tickers to check for user {user.State.Email}"
            return None
        | _ ->
            logger.LogInformation $"Checking {tickers.Length} tickers for user {user.State.Email}"

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

    let sendEmailToUser (user: User) (tickerFilings: (Ticker * SECFilingRecord[]) array) = async {
        let! emailTickerFilings =
            tickerFilings
            |> Array.map (fun (ticker, filings) -> toEmailData ticker filings)
            |> Async.Sequential

        let emailData =
            {
                userName = if String.IsNullOrEmpty user.State.Firstname then user.State.Email else user.State.Firstname
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
            let! result = emails.SendSECFilings recipient sender subject emailData |> Async.AwaitTask
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

    let updateWatermarks (userId: string) (tickerFilings: (Ticker * SECFilingRecord[]) array) = task {
        let now = DateTimeOffset.UtcNow
        for ticker, _ in tickerFilings do
            do! secFilingStorage.UpsertWatermark userId ticker now
    }

    let executeInternal() = async {
        try
            logger.LogInformation "Starting SEC filings notification service"

            let! verifiedUsers = getVerifiedUsers accounts

            logger.LogInformation $"Processing {verifiedUsers.Length} verified users"

            let! results =
                verifiedUsers
                |> Seq.map processUserFilings
                |> Async.Sequential

            let usersWithFilings = results |> Array.choose id

            logger.LogInformation $"Found filings for {usersWithFilings.Length} user(s)"

            for user, tickerFilings in usersWithFilings do
                let! emailSent = sendEmailToUser user tickerFilings
                if emailSent then
                    do! updateWatermarks (user.State.Id.ToString()) tickerFilings |> Async.AwaitTask

            logger.LogInformation "SEC filings notification service completed successfully"
        with ex ->
            logger.LogError $"Error in SEC filings notification service: {ex.Message}"
    }

    interface IApplicationService

    member _.Execute() = task {
        do! executeInternal()
    }
