module core.fs.Brokerage.MonitoringServices

open System
open System.Threading.Tasks
open core.Account
open core.Shared
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Stocks

type AccountMonitoringService(
    accounts:IAccountStorage,
    portfolio:IPortfolioStorage,
    brokerage:IBrokerage,
    marketHours:IMarketHours,
    emailService:IEmailService,
    logger:ILogger) =
    
    // this is silly, but all kinds of approaches are not working to look up the ticker
    // in the db, so I maintain a list of common mappings hardcoded here
    let hardcodedQueryToTickerMapping =
        [
            ("WISDOMTREE FLOATING RATETREASRY ETF", "USFR")
            ("NEWMONT CORP", "NEM")
            ("GLOBAL PAYMENTS INC", "GPN")
            ("SOUTHWEST AIRLINES", "LUV")
            ("WILEY & SONS INC JOHN CLASS A", "WLY")
            ("KAISER ALUMINUM CORP", "KALU")
            ("AMERICAN STATES WTR", "AWR")
        ]
    
    let rec resolveTicker (user:UserState) searchQuery callCounter (t:AccountTransaction) = async {
        
        // first let's see if we have a hardcoded mapping
        let hardcodedMapping =
            hardcodedQueryToTickerMapping
            |> Seq.tryFind (fun (q, _) -> q = searchQuery)
            
        match hardcodedMapping with
        | Some (_, ticker) ->
            let newTransaction = { t with Ticker = ticker |> Ticker |> Some }
            return Some newTransaction
        | None ->
            match callCounter with
            | x when x >= 10 -> return None
            | _ ->
                // see if we can use description to resolve the ticker
                let! result = brokerage.Search user SearchQueryType.Description searchQuery 10 |> Async.AwaitTask
                match result with
                | Error e ->
                    logger.LogInformation $"Unable to resolve ticker for {t.Description}: {e.Message}"
                    return None
                | Ok searchResults ->
                    match searchResults with
                    | [||] ->
                        logger.LogInformation $"No results found for {t.Description}"
                        // let's split description by space, and try to concat all but the last word and try again
                        let words = searchQuery.Split [|' '|]
                        let newDescription = words |> Array.take (words.Length - 1) |> String.concat " "
                        return! resolveTicker user newDescription (callCounter + 1) t
                    | [|single|] ->
                        let newTransaction = { t with Ticker = Some single.Symbol }
                        return Some newTransaction
                    | _ ->
                        logger.LogInformation $"Resolved ticker for {searchQuery} to {searchResults.Length} results"
                        searchResults
                        |> Array.iter (fun r -> logger.LogInformation $"\tResolved ticker for {t.Description} to {r.Symbol} - {r.AssetType} - {r.SecurityName}")
                        return None
    }
    
    let processTransaction (user:UserState) (t:AccountTransaction) = async {
        let! stock = portfolio.GetStockPositions (user.Id |> UserId) |> Async.AwaitTask
        
        // find stock position that matches the ticker and was opened before the transaction
        let previousPosition =
            stock
            |> Seq.tryFind (fun p -> p.Ticker = t.Ticker.Value && p.Opened < t.TradeDate)
            
        match previousPosition with
        | None -> return None
        | Some position ->
            
            let appliedPosition =
                match t.Type with
                | AccountTransactionType.Dividend ->
                    position |> StockPosition.processDividend t.TransactionId t.TradeDate t.Description t.NetAmount
                | AccountTransactionType.Fee ->
                    position |> StockPosition.processFee t.TransactionId t.TradeDate t.Description t.NetAmount
                | _ ->
                    failwith $"Not sure how to process transaction of type {t.Type} on {position.Ticker}"
            
            do! appliedPosition |> portfolio.SaveStockPosition (user.Id |> UserId) previousPosition |> Async.AwaitTask
            
            let appliedTransaction = { t with Applied = Some DateTimeOffset.Now }
            
            return Some appliedTransaction
    }
    
    interface core.fs.IApplicationService
    
    member _.RunTransactionProcessing() = task {
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! users =
            pairs
            |> Seq.map (fun pair -> pair.Id |> accounts.GetUser |> Async.AwaitTask)
            |> Async.Sequential
            
        let connectedUsers =
            users
            |> Seq.choose id
            |> Seq.filter _.State.ConnectedToBrokerage
            
        let! _ =
            connectedUsers
            |> Seq.map (fun user -> async {
                logger.LogInformation $"Processing transactions for {user.State.Id}"
                    
                let! transactions = accounts.GetAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                logger.LogInformation $"Found {transactions |> Seq.length} transactions for {user.State.Id}"
                    
                let! transactionsWithDiscoveredTickers =
                    transactions
                    |> Seq.filter (fun t -> t.Applied.IsNone)
                    |> Seq.filter (fun t -> t.Ticker.IsNone)
                    |> Seq.map(fun t -> resolveTicker user.State t.Description 0 t)
                    |> Async.Sequential
                    
                let unprocessedWithTickers =
                    transactionsWithDiscoveredTickers
                    |> Array.choose id
                    |> Seq.append (
                        transactions
                        |> Seq.filter (fun t -> t.Applied.IsNone)
                        |> Seq.filter (fun t -> t.Ticker.IsSome)
                    )
                    
                // for each transaction that has a ticker, we can apply it based on what it is
                let! appliedTransactionOptions =
                    unprocessedWithTickers
                    |> Seq.map(processTransaction user.State) 
                    |> Async.Sequential
                
                let appliedTransactions = appliedTransactionOptions |> Array.choose id
                
                logger.LogInformation $"Applied {appliedTransactions.Length} transactions for {user.State.Id}"
                
                // save applied transactions
                do! appliedTransactions |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // reload transactions
                let! transactions = accounts.GetAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // and any that are unprocessed and interest, mark them as applied as right now we treat them as no-op
                let appliedInterestTransactions =
                    transactions
                    |> Seq.filter (fun t -> t.Applied.IsNone)
                    |> Seq.filter (fun t -> t.Type = AccountTransactionType.Interest)
                    |> Seq.map (fun t -> { t with Applied = Some DateTimeOffset.Now })
                    |> Seq.toArray
                    
                do! appliedInterestTransactions |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // send a list of applied transactions to the user
                let appliedDescriptions =
                    appliedTransactions
                    |> Seq.map (fun t -> $"{t.Type}: {t.Ticker} - {t.Description}")
                    |> Seq.toArray
                    
                match appliedDescriptions with
                | [||] -> ()
                | _ ->
                    let emailInput = {EmailInput.Body = appliedDescriptions |> String.concat "\n"; Subject = "Applied Transactions"; To = user.State.Email; From = Sender.Support.Email; FromName = Sender.Support.Name }
                    let! _ = emailService.SendWithInput emailInput |> Async.AwaitTask
                    ()
                    
            })
            |> Async.Sequential
            
        return Task.CompletedTask
    }
    member _.RunAccountValueOrderAndTransactionSync() = task {
        
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! users =
            pairs
            |> Seq.map (fun pair -> pair.Id |> accounts.GetUser |> Async.AwaitTask)
            |> Async.Sequential
            
        let connectedUsers =
            users
            |> Seq.choose id
            |> Seq.filter _.State.ConnectedToBrokerage
            
        let! _ =
            connectedUsers
            |> Seq.map (fun user -> async {
                    
                let! account = brokerage.GetAccount user.State |> Async.AwaitTask
                match account with
                | Error e ->
                    logger.LogError $"Unable to get brokerage account for {user.State.Id}: {e.Message}"
                | Ok account ->
                    let cash = account.CashBalance
                    let equity = account.Equity
                    let longValue = account.LongMarketValue
                    let shortValue = account.ShortMarketValue
                    let marketNow = marketHours.ToMarketTime DateTime.UtcNow |> _.ToString("yyyy-MM-dd")
                    let snapshot = AccountBalancesSnapshot(cash.Value, equity.Value, longValue.Value, shortValue.Value, marketNow)
                    do! snapshot |> accounts.SaveAccountBalancesSnapshot (user.State.Id |> UserId) |> Async.AwaitTask
                    
                    // save orders
                    do! account.Orders |> accounts.SaveAccountBrokerageOrders (user.State.Id |> UserId) |> Async.AwaitTask
                    
                    logger.LogInformation $"Saved balances for {user.State.Id}: {cash} {equity} {shortValue} {longValue}"
                    
                    // let's do transactions
                    let! transactions = brokerage.GetTransactions user.State [|AccountTransactionType.Dividend; AccountTransactionType.Interest; AccountTransactionType.Fee|] |> Async.AwaitTask
                    
                    match transactions with
                    | Error e ->
                        logger.LogError $"Unable to get brokerage transactions for {user.State.Id}: {e.Message}"
                    | Ok transactions ->
                        do! transactions |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                        logger.LogInformation $"Saved transactions for {user.State.Id}: {transactions.Length} transactions"
                    ()
                    
            })
            |> Async.Sequential
            
        return Task.CompletedTask
    }
