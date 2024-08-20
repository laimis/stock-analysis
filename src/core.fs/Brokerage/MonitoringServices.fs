module core.fs.Brokerage.MonitoringServices

open System
open System.Threading.Tasks
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage

type AccountMonitoringService(
    accounts:IAccountStorage,
    brokerage:IBrokerage,
    marketHours:IMarketHours,
    logger:ILogger) =
    
    interface core.fs.IApplicationService
    
    member _.Execute() = task {
        
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
                    
            })
            |> Async.Sequential
            
        return Task.CompletedTask
    }
