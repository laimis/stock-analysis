namespace core.fs.Accounts

open System
open System.Threading
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage

// running this on a sechedule to refresh brokerage connections
// the issue with Schwab is that they no longer allowing to renew
// so I changed this job to email the user instead and tell them to go ahead and refresh the token    
type RefreshBrokerageConnectionService(accounts:IAccountStorage,brokerage:IBrokerage,logger:ILogger,email:IEmailService) =
    
    let runBrokerageCheck userId = task {
        
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
                    logger.LogInformation $"Brokerage connection for user {userId} is about to expire. Sending notice..."
                    
                    let refreshUrl = "https://app.nightingaletrading.com/profile/"
                    
                    let message = $"Your brokerage connection is about to expire. Please refresh your connection by going to <a href=\"{refreshUrl}\">this link</a>."
                    
                    let emailInput = {EmailInput.Body = message; From = Sender.NoReply.Email; Subject = "Refresh Brokerage Account"; To = user.State.Email; FromName = Sender.NoReply.Name }
                    
                    do! email.SendWithInput emailInput
                    // let! refreshedToken = brokerage.RefreshAccessToken user.State
                    //
                    // match refreshedToken.IsError with
                    // | true ->
                    //     logger.LogError $"Brokerage connection for user {userId} could not be refreshed. Error: {refreshedToken.error}"
                    // | false ->
                    //     user.RefreshBrokerageConnection refreshedToken.access_token refreshedToken.refresh_token refreshedToken.token_type refreshedToken.expires_in refreshedToken.scope
                    //     do! accounts.Save user
                    //     logger.LogInformation $"Brokerage connection for user {userId} was refreshed."
                | false ->
                    logger.LogInformation $"Brokerage connection for user {userId} is valid until {user.State.BrokerageRefreshTokenExpires}. Skipping refresh."  
    }
    
    interface IApplicationService
    
    member _.Execute() = task {
            
        let! users = accounts.GetUserEmailIdPairs()
        
        let! _ =
            users
            |> Seq.map (fun emailIdPair ->
                runBrokerageCheck emailIdPair.Id |> Async.AwaitTask)
            |> Async.Sequential
            |> Async.StartAsTask
            
        ()
    }
