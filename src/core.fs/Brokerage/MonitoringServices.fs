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
            ("WISDOMTREE FLOATING RTETREAS ETF IV", "USFR")
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
        | true -> { t with InferredType = Some inferredType } |> Ok
        | false -> t |> Error
    
    let rec resolveTicker (user:UserState) searchQuery callCounter (t:AccountTransaction) = async {
        match t.InferredTicker with
        | Some _ -> return t |> Ok
        | None ->
            // first let's see if we have a hardcoded mapping
            let hardcodedMapping =
                hardcodedQueryToTickerMapping
                |> Seq.tryFind (fun (q, _) -> q = searchQuery)
            
            match hardcodedMapping with
            | Some (_, ticker) ->
                let newTransaction = { t with InferredTicker = ticker |> core.Shared.Ticker |> Some }
                return newTransaction |> Ok
            | None ->
                
                // let's see if the type is fee or dividend, then attempt to resovle the ticker from description by splitting ~ and taking second member
                let isFeeOrDividend = t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Fee || t.InferredType.Value = AccountTransactionType.Dividend
                let hasTilde = t.Description.Contains("~")
                
                match isFeeOrDividend && hasTilde with
                | true ->
                    let ticker = t.Description.Split "~" |> Array.item 1
                    let newTransaction = { t with InferredTicker = ticker |> core.Shared.Ticker |> Some }
                    return newTransaction |> Ok
                | false ->
                    // search only if call counter is less than 10 or transaction is not interest
                    let stopCondition = t.InferredType.Value = AccountTransactionType.Interest || callCounter >= 10
                    match stopCondition with
                    | true -> return t |> Error
                    | false ->
                        // see if we can use description to resolve the ticker
                        let! result = brokerage.Search user SearchQueryType.Description searchQuery 10 |> Async.AwaitTask
                        match result with
                        | Error e ->
                            logger.LogInformation $"Unable to resolve ticker for {t.Description}: {e.Message}"
                            return t |> Error
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
                                return newTransaction |> Ok
                            | _ ->
                                logger.LogInformation $"Resolved ticker for {searchQuery} to {searchResults.Length} results"
                                searchResults
                                |> Array.iter (fun r -> logger.LogInformation $"\tResolved ticker for {t.Description} to {r.Symbol} - {r.AssetType} - {r.SecurityName}")
                                return t |> Error
    }
    
    let applyInterest (user:User) (t:AccountTransaction) = async {
        
        // reload user account to have latest state
        let! account = user.State.Id |> UserId |> accounts.GetUser |> Async.AwaitTask
        
        match account with
        | None -> return t |> Error
        | Some account ->
            account.ApplyBrokerageInterest t.TradeDate t.TransactionId t.NetAmount
            do! account |> accounts.Save |> Async.AwaitTask
            return {t with Applied = DateTimeOffset.UtcNow |> Some } |> Ok
    }
    
    let applyCashTransfer (user:User) (t:AccountTransaction) = async {
        let! account = user.State.Id |> UserId |> accounts.GetUser |> Async.AwaitTask
        
        match account with
        | None -> return t |> Error
        | Some account ->
            account.ApplyCashTransfer t.TradeDate t.TransactionId t.NetAmount
            do! account |> accounts.Save |> Async.AwaitTask
            return {t with Applied = DateTimeOffset.UtcNow |> Some } |> Ok
    }
    
    let processTransaction (user:UserState) (t:AccountTransaction) = async {
        let! stock = portfolio.GetStockPositions (user.Id |> UserId) |> Async.AwaitTask
        
        // find stock position that matches the ticker and was opened before the transaction
        let previousPosition =
            stock
            |> Seq.tryFind (fun p -> p.Ticker = t.InferredTicker.Value && p.Opened < t.TradeDate)
            
        match previousPosition with
        | None -> return t |> Error
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
            
            return appliedTransaction |> Ok
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
                    
                logger.LogInformation $"Resolved types for {unappliedWithTypesResolved |> Seq.length} transactions for {user.State.Id}"
                    
                let failedToResolveTypes =
                    unappliedWithTypesResolved
                    |> Seq.choose (fun t -> match t with | Ok _ -> None | Error e -> Some e)
                    
                // we will take interest transactions and apply it on the user's account
                // and dividend transactions will be applied to the stock positions
                let! interestTransactions =
                    unappliedWithTypesResolved
                    |> Seq.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                    |> Seq.filter (fun t -> t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Interest)
                    |> Seq.map (applyInterest user)
                    |> Async.Sequential
                    
                let failedToApplyInterest =
                    interestTransactions
                    |> Seq.choose (fun t -> match t with | Ok _ -> None | Error e -> Some e)
                    
                let appliedInterest =
                    interestTransactions
                    |> Array.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                    
                let! cashTransfers =
                    unappliedWithTypesResolved
                    |> Seq.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                    |> Seq.filter (fun t -> t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Transfer)
                    |> Seq.map(applyCashTransfer user)
                    |> Async.Sequential
                    
                let failedToApplyCashTransfers =
                    cashTransfers
                    |> Seq.choose (fun t -> match t with | Ok _ -> None | Error e -> Some e)
                    
                let appliedCashTransfers =
                    cashTransfers
                    |> Array.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                
                // the brokerage transactions do not have tickers most of the time, so we need to resolve them
                // if possible, using search approach or hardcoded mappings
                let! dividendTransactionsWithResolveAttempt =
                    unappliedWithTypesResolved
                    |> Seq.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                    |> Seq.filter (fun t -> t.InferredType.IsSome && t.InferredType.Value = AccountTransactionType.Dividend)
                    |> Seq.map(fun t -> resolveTicker user.State t.Description 0 t)
                    |> Async.Sequential
                    
                let failedToResolveTickers =
                    dividendTransactionsWithResolveAttempt
                    |> Seq.choose (fun t -> match t with | Ok _ -> None | Error e -> Some e)
                    
                // for each transaction that has a ticker, we can process it
                let! appliedDividendsResults =
                    dividendTransactionsWithResolveAttempt
                    |> Seq.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                    |> Seq.map(processTransaction user.State) 
                    |> Async.Sequential
                
                let failedToApplyDividends =
                    appliedDividendsResults
                    |> Seq.choose (fun t -> match t with | Ok _ -> None | Error e -> Some e)
                    
                let appliedDividends =
                    appliedDividendsResults
                    |> Array.choose (fun t -> match t with | Ok t -> Some t | Error _ -> None)
                
                // save applied dividend transactions
                logger.LogInformation $"Applied {appliedDividends.Length} dividend transactions for {user.State.Id}"
                do! appliedDividends |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // save applied interest
                logger.LogInformation $"Applied {appliedInterest.Length} interest transactions for {user.State.Id}"
                do! appliedInterest |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                // save applied cash transfers
                logger.LogInformation $"Applied {appliedCashTransfers.Length} cash transfer transactions for {user.State.Id}"
                do! appliedCashTransfers |> accounts.SaveAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                
                let getTickerOrBlank (t:AccountTransaction) =
                    match t.InferredTicker with
                    | Some ticker -> ticker.Value
                    | None -> ""

                // send a list of applied transactions to the user
                let appliedDividendsMapped =
                    appliedDividends
                    |> Seq.map (fun t -> {|ticker = t |> getTickerOrBlank; netAmount = t.NetAmount; description = t.Description |})
                    |> Seq.toArray
                    
                let appliedInterestMapped =
                    appliedInterest
                    |> Seq.map (fun t -> {|description = t.Description; netAmount = t.NetAmount|})
                    |> Seq.toArray
                    
                let appliedCashTransfersMapped =
                    appliedCashTransfers
                    |> Seq.map (fun t -> {|description = t.Description; netAmount = t.NetAmount|})
                    |> Seq.toArray
                    
                // now combine all failed results and send them to the user
                let failedResults =
                    failedToResolveTypes
                    |> Seq.map (fun t -> {|netAmount = t.NetAmount; description = $"Failed to resolve type: {t.Description}"|})
                    |> Seq.append
                        (failedToResolveTickers
                        |> Seq.map (fun t -> {|netAmount = t.NetAmount; description = $"Failed to resolve ticker for {t.Description}"|})
                        )
                    |> Seq.append
                        (failedToApplyDividends
                        |> Seq.map (fun t -> {|netAmount = t.NetAmount; description = $"Failed to apply dividend: {t.Description}"|})
                        )
                    |> Seq.append 
                        (failedToApplyInterest
                        |> Seq.map (fun t -> {|netAmount = t.NetAmount; description = $"Failed to apply interest: {t.Description}"|})
                        )
                    |> Seq.append
                        (failedToApplyCashTransfers
                        |> Seq.map (fun t -> {|netAmount = t.NetAmount; description = $"Failed to apply cash transfer: {t.Description}"|})
                        )
                    |> Seq.toArray
                    
                match appliedDividendsMapped, appliedInterestMapped, appliedCashTransfersMapped, failedResults with
                | [||], [||], [||], [||] -> ()
                | _ ->
                    
                    let payload = {|
                                    appliedDividends = appliedDividendsMapped
                                    appliedInterest = appliedInterestMapped
                                    appliedCashTransfers = appliedCashTransfersMapped
                                    failures = failedResults
                                    |}

                    let recipient = Recipient(user.State.Email, user.State.Name)
                    let sender = Sender.Support

                    let! _ = emailService.SendWithTemplate recipient sender EmailTemplate.BrokerageTransactions payload |> Async.AwaitTask
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
                    
                    do! account.StockOrders |> accounts.SaveAccountBrokerageStockOrders (user.State.Id |> UserId) |> Async.AwaitTask
                    do! account.OptionOrders |> accounts.SaveAccountBrokerageOptionOrders (user.State.Id |> UserId) |> Async.AwaitTask
                    
                    logger.LogInformation $"Saved balances for {user.State.Id}: {cash} {equity} {shortValue} {longValue}"
                    
                    // let's do transactions
                    let! transactions =
                        [|AccountTransactionType.Dividend; AccountTransactionType.Interest; AccountTransactionType.Fee; AccountTransactionType.Transfer|]
                        |> brokerage.GetTransactions user.State
                        |> Async.AwaitTask
                    
                    match transactions with
                    | Error e ->
                        logger.LogError $"Unable to get brokerage transactions for {user.State.Id}: {e.Message}"
                    | Ok transactions ->
                        logger.LogInformation $"Got {transactions.Length} transactions for {user.State.Id}"
                        do! transactions |> accounts.InsertAccountBrokerageTransactions (user.State.Id |> UserId) |> Async.AwaitTask
                        logger.LogInformation $"Saved transactions for {user.State.Id}: {transactions.Length} transactions"
                    ()
                    
            })
            |> Async.Sequential
            
        return Task.CompletedTask
    }
