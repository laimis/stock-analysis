namespace secedgar.fs

open System
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Accounts
open core.fs.Adapters.SEC
open core.fs.Adapters.Storage
open core.Shared

// ─────────────────────────────────────────────────────────────────────────────
// Job: sync SEC filings from the SEC API into the database, then immediately
// parse any newly saved ownership-relevant filings (13G, 13D, 144).
//
// Runs every 10 minutes during market hours.  The three catch-up processing
// services (Schedule13G/13D, Form144) handle any filings that failed or were
// skipped here and run once or twice daily.
// ─────────────────────────────────────────────────────────────────────────────

type SECFilingsSyncService(
    accounts: IAccountStorage,
    portfolio: IPortfolioStorage,
    secFilings: ISECFilings,
    secFilingStorage: ISECFilingStorage,
    schedule13G: Schedule13GProcessingService,
    form144: Form144ProcessingService,
    schedule13D: Schedule13DProcessingService,
    logger: ILogger<SECFilingsSyncService>) =

    // ── helpers ──────────────────────────────────────────────────────────────

    let getUserTickers (userId: UserId) = task {
        let! stocks = portfolio.GetStockPositions userId
        let stockTickers = stocks |> Seq.filter _.IsOpen |> Seq.map _.Ticker

        let! options = portfolio.GetOptionPositions userId
        let optionTickers = options |> Seq.filter _.IsOpen |> Seq.map _.UnderlyingTicker

        return
            Seq.append stockTickers optionTickers
            |> Seq.distinct
            |> Seq.toArray
    }

    let getVerifiedUsers () = async {
        let! userPairs = accounts.GetUserEmailIdPairs() |> Async.AwaitTask
        let! users =
            userPairs
            |> Seq.map (fun pair -> async { return! accounts.GetUser pair.Id |> Async.AwaitTask })
            |> Async.Sequential
        return users |> Array.choose id |> Array.filter (fun u -> u.State.Verified.HasValue)
    }

    let isOwnershipForm (formType: string) =
        let upper = formType.ToUpperInvariant()
        upper.Contains "13G" || upper.Contains "13D" || upper = "144" || upper = "144/A"

    // Dispatches a single filing to the appropriate parser.
    // Each parser is idempotent so calling it on an already-processed filing is safe.
    let parseOwnershipFiling (filing: SECFilingRecord) = task {
        let upper = filing.FormType.ToUpperInvariant()
        let! result =
            if upper.Contains "13G" then schedule13G.ProcessFiling filing
            elif upper.Contains "13D" then schedule13D.ProcessFiling filing
            else form144.ProcessFiling filing  // 144 / 144/A

        match result with
        | Ok () -> ()
        | Error msg ->
            logger.LogWarning(
                "Ownership parsing did not succeed for {FormType} filing {FilingUrl}: {Reason}",
                filing.FormType, filing.FilingUrl, msg)

        return result
    }

    // ── core logic ────────────────────────────────────────────────────────────

    let fetchAndSaveFilings (ticker: Ticker) = async {
        try
            let! response = secFilings.GetFilings ticker |> Async.AwaitTask
            match response with
            | Error err when err.Message.StartsWith("No CIK mapping found") ->
                logger.LogDebug("Skipping SEC filings sync for {Ticker}: no CIK mapping (likely ETF or fund)", ticker.Value)
            | Error err ->
                logger.LogError("Failed to get SEC filings for {Ticker}: {Error}", ticker.Value, err.Message)
            | Ok companyFilings when companyFilings.Filings.Length > 0 ->
                let! cikMapping = accounts.GetTickerCik(ticker.Value) |> Async.AwaitTask
                let cik =
                    match cikMapping with
                    | Some mapping -> mapping.Cik
                    | None -> "unknown"

                let filingRecords =
                    companyFilings.Filings
                    |> Seq.map (SECFilingRecord.fromCompanyFiling ticker cik)
                    |> Seq.toArray

                let! inserted = secFilingStorage.SaveFilings filingRecords |> Async.AwaitTask
                let insertedArray = inserted |> Seq.toArray
                logger.LogInformation(
                    "Synced {Ticker}: {New} new, {Total} total",
                    ticker.Value, insertedArray.Length, companyFilings.Filings.Length)

                // Immediately parse ownership-relevant forms from the newly inserted records
                // so that SECFilingsMonitoringService always sees enriched ownership data.
                // We use insertedArray (not the full API response) so we only parse filings
                // that were genuinely new — avoiding wasted SEC HTTP calls for already-parsed forms.
                let ownershipForms =
                    insertedArray |> Array.filter (fun f -> isOwnershipForm f.FormType)

                let! _ =
                    ownershipForms
                    |> Seq.map (fun f -> async { return! parseOwnershipFiling f |> Async.AwaitTask })
                    |> Async.Sequential

                return ()
            | Ok _ -> logger.LogInformation("No new SEC filings for {Ticker}", ticker.Value)
        with ex ->
            logger.LogError(ex, "Error syncing SEC filings for {Ticker}: {Message}", ticker.Value, ex.Message)
    }

    let executeInternal () = async {
        try
            logger.LogInformation "Starting SEC filings sync service"

            let! verifiedUsers = getVerifiedUsers ()
            logger.LogInformation("Collecting tickers from {Count} verified user(s)", verifiedUsers.Length)

            let! allTickers =
                verifiedUsers
                |> Seq.map (fun u -> async {
                    return! getUserTickers (UserId u.State.Id) |> Async.AwaitTask
                })
                |> Async.Sequential

            let uniqueTickers =
                allTickers
                |> Seq.concat
                |> Seq.distinctBy _.Value
                |> Seq.toArray

            logger.LogInformation("Syncing SEC filings for {Count} unique ticker(s)", uniqueTickers.Length)

            do! uniqueTickers |> Seq.map fetchAndSaveFilings |> Async.Sequential |> Async.Ignore

            logger.LogInformation "SEC filings sync service completed successfully"
        with ex ->
            logger.LogError(ex, "Error in SEC filings sync service: {Message}", ex.Message)
    }

    interface IApplicationService

    member _.Execute() = task {
        do! executeInternal ()
    }

    /// On-demand sync for a single ticker: fetches new filings from EDGAR, then
    /// processes all existing unprocessed ownership forms for that ticker.
    /// Idempotent — parsers skip already-processed filings.
    member _.SyncOwnershipForTicker(symbol: string) = task {
        let ticker = Ticker symbol
        logger.LogInformation("Starting on-demand ownership sync for {Ticker}", symbol)

        // Fetch new filings from SEC and parse any newly inserted ownership forms inline
        do! fetchAndSaveFilings ticker

        // Also attempt to parse any existing DB filings for this ticker that were never
        // processed (e.g. ticker was not in any portfolio at time of original sync)
        let! existingFilings = secFilingStorage.GetFilingsByTicker ticker
        let ownershipForms =
            existingFilings
            |> Seq.filter (fun f -> isOwnershipForm f.FormType)
            |> Seq.toArray

        let! _ =
            ownershipForms
            |> Seq.map (fun f -> async { return! parseOwnershipFiling f |> Async.AwaitTask })
            |> Async.Sequential

        logger.LogInformation("Completed on-demand ownership sync for {Ticker}", symbol)
    }
