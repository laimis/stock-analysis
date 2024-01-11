module core.fs.Brokerage.MonitoringServices

open System
open System.Threading
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage

type AccountMonitoringService(
    accounts:IAccountStorage,
    brokerage:IBrokerage,
    marketHours:IMarketHours) =
    
    interface core.fs.IApplicationService
    
    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {
        
        let! pairs = accounts.GetUserEmailIdPairs()
        
        return!
            pairs
            |> Seq.takeWhile (fun _ -> not cancellationToken.IsCancellationRequested)
            |> Seq.map (fun pair -> async {
                let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
                match user with
                | None ->
                    return ()
                | Some user ->
                    if user.State.ConnectedToBrokerage then
                        let! account = brokerage.GetAccount user.State |> Async.AwaitTask
                        let result = account.Result
                        
                        match result with
                        | Error e ->
                            logger.LogError $"Unable to get brokerage account for {user.State.Id}: {e.Message}"
                        | Ok account ->
                            let cash = account.CashBalance
                            let equity = account.Equity
                            let longValue = account.LongMarketValue
                            let shortValue = account.ShortMarketValue
                            let marketNow = marketHours.ToMarketTime DateTime.UtcNow
                            let snapshot = AccountBalancesSnapshot(cash.Value, equity.Value, longValue.Value, shortValue.Value, marketNow.DateTime, user.State.Id)
                            do! accounts.SaveAccountBalancesSnapshot (user.State.Id |> UserId) snapshot |> Async.AwaitTask
                            logger.LogInformation $"Saved balances for {user.State.Id}: {cash} {equity} {shortValue} {longValue}"
                            
                            return ()
                        
                    return ()
            })
            |> Async.Sequential
    }
    
    member _.NextRun now =
        // we always want to run after market closes
        let nowInMarketTime = marketHours.ToMarketTime now
        let nextRun =
            nowInMarketTime.Date.AddDays(1.0).AddHours(16)
            |> marketHours.ToUniversalTime
        nextRun