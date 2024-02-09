module core.fs.Brokerage.MonitoringServices

open System
open System.Threading
open System.Threading.Tasks
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
        
        let! users =
            pairs
            |> Seq.takeWhile (fun _ -> not cancellationToken.IsCancellationRequested)
            |> Seq.map (fun pair -> pair.Id |> accounts.GetUser |> Async.AwaitTask)
            |> Async.Sequential
            
        let connectedUsers =
            users
            |> Seq.choose id
            |> Seq.filter _.State.ConnectedToBrokerage
            
        let! _ =
            connectedUsers
            |> Seq.takeWhile (fun _ -> not cancellationToken.IsCancellationRequested)
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
                    let snapshot = AccountBalancesSnapshot(cash.Value, equity.Value, longValue.Value, shortValue.Value, marketNow, user.State.Id)
                    do! accounts.SaveAccountBalancesSnapshot (user.State.Id |> UserId) snapshot |> Async.AwaitTask
                    logger.LogInformation $"Saved balances for {user.State.Id}: {cash} {equity} {shortValue} {longValue}"
                    
            })
            |> Async.Sequential
            
        return Task.CompletedTask
    }
    
    member _.NextRun now =
        // we always want to run after market closes
        let nowInMarketTime = marketHours.ToMarketTime now
        let nextRun =
            nowInMarketTime.Date.AddDays(1.0).AddHours(16)
            |> marketHours.ToUniversalTime
        nextRun
