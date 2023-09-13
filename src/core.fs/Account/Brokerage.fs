module core.fs.Account.Brokerage

    open System
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.Shared.Adapters.Storage
    open core.fs

    type Connect = struct end
    
    [<Struct>]
    type ConnectCallback = {
        Code:string
        UserId:Guid
    }
    
    [<Struct>]
    type Disconnect(userId:Guid) =
        member _.UserId = userId
        
    [<Struct>]
    type Info(userId:Guid) =
        member _.UserId = userId
    
    type Handler(accounts:IAccountStorage, brokerage:IBrokerage) = 
        
        interface IApplicationService
        
        member this.HandleConnect(_:Connect) = 
            brokerage.GetOAuthUrl()
            
        member this.HandleConnectCallback(cmd:ConnectCallback) = task {
            let! r = brokerage.ConnectCallback(cmd.Code)
            
            let! user = accounts.GetUser(cmd.UserId)
            
            user.ConnectToBrokerage(r.access_token, r.refresh_token, r.token_type, r.expires_in, r.scope, r.refresh_token_expires_in)
            
            let! _ = accounts.Save(user)
            
            return ServiceResponse()
        }
        
        member this.HandleDisconnect(cmd:Disconnect) = task {
            let! user = accounts.GetUser(cmd.UserId)
            user.DisconnectFromBrokerage()
            let! _ = accounts.Save(user)
            return ServiceResponse()
        }
        
        member this.HandleInfo(cmd:Info) = task {
            let! user = accounts.GetUser(cmd.UserId);

            let! oauth = brokerage.GetAccessToken(user.State)
            
            return ServiceResponse<OAuthResponse>(oauth)
        }
           
            