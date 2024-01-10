namespace core.fs.Accounts

open System
open System.Threading
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
    
type RefreshBrokerageConnectionService(accounts:IAccountStorage,brokerage:IBrokerage) =
    
    let runBrokerageCheck (logger:ILogger) (cancellationToken:CancellationToken) userId = task {
        
        let! user = accounts.GetUser userId
        match user with
        | None -> ()
        | Some user ->
            let expirationThreshold = DateTimeOffset.UtcNow.Date.AddDays(1.0)
            
            match user.State.ConnectedToBrokerage with
            | false ->
                logger.LogInformation $"User {userId} is not connected to a brokerage. Skipping refresh."
            | true ->
                match user.State.BrokerageRefreshTokenExpires.Date <= expirationThreshold with
                | true ->
                    logger.LogInformation $"Brokerage connection for user {userId} is about to expire. Refreshing..."
                    
                    let! refreshedToken = brokerage.RefreshAccessToken user.State
                    
                    match refreshedToken.IsError with
                    | true ->
                        logger.LogError $"Brokerage connection for user {userId} could not be refreshed. Error: {refreshedToken.error}"
                    | false ->
                        user.RefreshBrokerageConnection refreshedToken.access_token refreshedToken.refresh_token refreshedToken.token_type refreshedToken.expires_in refreshedToken.scope refreshedToken.refresh_token_expires_in
                        do! accounts.Save user
                        logger.LogInformation $"Brokerage connection for user {userId} was refreshed."
                | false ->
                    logger.LogInformation $"Brokerage connection for user {userId} is valid until {user.State.BrokerageRefreshTokenExpires}. Skipping refresh."  
    }
    
    interface IApplicationService
    
    member _.Execute (logger:ILogger) cancellationToken = task {
            
        let! users = accounts.GetUserEmailIdPairs()
        
        let! _ =
            users
            |> Seq.map (fun emailIdPair ->
                runBrokerageCheck logger cancellationToken emailIdPair.Id |> Async.AwaitTask)
            |> Async.Sequential
            |> Async.StartAsTask
            
        ()
    }
    
    member _.NextRunTime (now:DateTimeOffset) = now.AddHours(12.0)