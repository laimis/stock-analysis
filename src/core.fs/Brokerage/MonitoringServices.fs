module core.fs.Brokerage.MonitoringServices

open System
open System.Threading.Tasks
open core.Account
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Stocks

//
 // match t.description with
 //                                    | x when x.Contains("Dividend~") -> 
 //                                        Dividend, Some(x.Split("~")[1])
 //                                    | x when x.Contains("Dividend Short Sale~") -> 
 //                                        Dividend, Some(x.Split("~")[1])
 //                                    | x when x.Contains("ADR Fees~") -> 
 //                                        Fee, Some(x.Split("~")[1])
 //                                    | x when x.Contains(" Interest ") ->
 //                                        Interest, None
 //                                    | x when x.Contains("SCHWAB1 INT ") ->
 //                                        Interest, None
 //                                    | _ ->
 //                                        match t.``type`` with
 //                                        | "TRADE" -> Trade, None
 //                                        | "DIVIDEND_OR_INTEREST" -> Dividend, None
 //                                        | "ACH_RECEIPT" -> Transfer, None
 //                                        | "ACH_DISBURSEMENT" -> Transfer, None
 //                                        | "CASH_RECEIPT" -> Transfer, None
 //                                        | "CASH_DISBURSEMENT" -> Transfer, None
 //                                        | "ELECTRONIC_FUND" -> Transfer, None
 //                                        | "WIRE_OUT" -> Transfer, None
 //                                        | "WIRE_IN" -> Transfer, None
 //                                        | _ -> Other, None
 
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
        
    let resolveType (t:AccountTransaction) =
        let inferredType =
            match t.BrokerageType with
            | "TRADE" -> Trade
            | "DIVIDEND_OR_INTEREST" ->
                match t.Description with
                | x when x.Contains("Dividend~") -> AccountTransactionType.Dividend
                | x when x.Contains("Dividend Short Sale~") -> AccountTransactionType.Dividend
                | x when x.Contains("ADR Fees~") -> AccountTransactionType.Fee
                | x when x.Contains("Foreign Tax Withheld~") -> AccountTransactionType.Fee
                | x when x.Contains(" Interest ") -> Interest
                | x when x.Contains("SCHWAB1 INT ") -> Interest
                | _ -> AccountTransactionType.Dividend
            | "ACH_RECEIPT" -> Transfer
            | "ACH_DISBURSEMENT" -> Transfer
            | "CASH_RECEIPT" -> Transfer
            | "CASH_DISBURSEMENT" -> Transfer
            | "ELECTRONIC_FUND" -> Transfer
            | "WIRE_OUT" -> Transfer
            | "WIRE_IN" -> Transfer
            | _ -> Other
        
        match t.InferredType.IsNone || inferredType <> t.InferredType.Value with
        | true -> { t with InferredType = Some inferredType }
        | false -> t
    
    let rec resolveTicker (user:UserState) searchQuery callCounter (t:AccountTransaction) = async {
        match t.InferredTicker with
        | Some _ -> return Some t
        | None ->
            // first let's see if we have a hardcoded mapping
            let hardcodedMapping =
                hardcodedQueryToTickerMapping
                |> Seq.tryFind (fun (q, _) -> q = searchQuery)
            
            match hardcodedMapping with
            | Some (_, ticker) ->
                let newTransaction = { t with InferredTicker = ticker |> core.Shared.Ticker |> Some }
                return Some newTransaction
            | None ->
                
                // let's see if the type is fee or dividend, then attempt to resovle the ticker from description by splitting ~ and taking second member
                let isFeeOrDividend = t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Fee || t.InferredType.Value = AccountTransactionType.Dividend
                let hasTilde = t.Description.Contains("~")
                
                match isFeeOrDividend && hasTilde with
                | true ->
                    let ticker = t.Description.Split "~" |> Array.item 1
                    let newTransaction = { t with InferredTicker = ticker |> core.Shared.Ticker |> Some }
                    return Some newTransaction
                | false ->
                    // search only if call counter is less than 10 or transaction is not interest
                    let stopCondition = t.InferredType.Value = AccountTransactionType.Interest || callCounter >= 10
                    match stopCondition with
                    | true -> return None
                    | false ->
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
                                logger.LogInformation $"Resolved ticker for {t.Description} to {single.Symbol} - {single.AssetType} - {single.SecurityName}"
                                let newTransaction = { t with InferredTicker = Some single.Symbol }
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
            |> Seq.tryFind (fun p -> p.Ticker = t.InferredTicker.Value && p.Opened < t.TradeDate)
            
        match previousPosition with
        | None -> return None
        | Some position ->
            
            let appliedPosition =
                match t.InferredType.Value with
                | AccountTransactionType.Dividend ->
                    position |> StockPosition.processDividend t.TransactionId t.TradeDate t.Description t.NetAmount
                | AccountTransactionType.Fee ->
                    position |> StockPosition.processFee t.TransactionId t.TradeDate t.Description t.NetAmount
                | _ ->
                    failwith $"Not sure how to process transaction of type {t.InferredType.Value} on {position.Ticker}"
            
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

                // first, get all transactions that have not been applied, and attempt to assing type
                let unappliedWithTypesResolved =
                    transactions
                    |> Seq.filter (fun t -> t.Applied.IsNone)
                    |> Seq.map(resolveType)
                
                // the brokerage transactions do not have tickers most of the time, so we need to resolve them
                // if possible, using search approach or hardcoded mappings
                let! unappliedWithTickers =
                    unappliedWithTypesResolved
                    |> Seq.map(fun t -> resolveTicker user.State t.Description 0 t)
                    |> Async.Sequential
                    
                // for each transaction that has a ticker, we can process it
                let! appliedTransactionOptions =
                    unappliedWithTickers
                    |> Seq.choose id
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
                    |> Seq.filter (fun t -> t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Interest)
                    |> Seq.map (fun t -> { t with Applied = Some DateTimeOffset.Now })
                    |> Seq.toArray
                    
                do! appliedInterestTransactions |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // send a list of applied transactions to the user
                let appliedDescriptions =
                    appliedTransactions
                    |> Seq.map (fun t -> $"{t.InferredTicker}: {t.BrokerageType} - {t.Description}")
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
