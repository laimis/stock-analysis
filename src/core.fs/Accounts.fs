namespace core.fs.Accounts

open System
open core.Account
open core.Shared

type UserId = UserId of Guid

module IdentifierHelper =
    
    let getUserId userId =
        match userId with
        | UserId id -> id
    
[<Struct>]
type EmailIdPair(email:string, id:string) =
    
    member _.Email = email
    member _.Id = id |> Guid |> UserId
    
type AccountBalancesSnapshot(cash:decimal,equity:decimal,longValue:decimal,shortValue:decimal,date:string,userId:Guid) =
    member _.Cash = cash
    member _.Equity = equity
    member _.LongValue = longValue
    member _.ShortValue = shortValue
    member _.Date = date
    member _.UserId = userId |> UserId
    
type ProcessIdToUserAssociation(Id:Guid, UserId:UserId, Timestamp:DateTimeOffset) =
    
    new(userId, timestamp:DateTimeOffset) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, timestamp)
    new(userId, timestamp:string) = ProcessIdToUserAssociation(Guid.NewGuid(), userId, DateTimeOffset.Parse(timestamp))
    new(id:Guid, userId:Guid, timestamp:string) = ProcessIdToUserAssociation(id, userId |> UserId, DateTimeOffset.Parse(timestamp))
    
    member _.IsOlderThan(duration:TimeSpan) = DateTimeOffset.UtcNow.Subtract(Timestamp) > duration
    member _.Id = Id
    member _.UserId = UserId
    member _.Timestamp = Timestamp
    
    
type User(events:System.Collections.Generic.IEnumerable<AggregateEvent>) =
    inherit Aggregate<UserState>(events)
    
    private new () = User([])
    
    static member Create (email:string,firstname:string,lastname:string) =
        // check if email is empty or null
        if String.IsNullOrWhiteSpace(email) then
            raise (ArgumentException(nameof(email)))
            
        // check if first name is empty or null
        if String.IsNullOrWhiteSpace(firstname) then
            raise (ArgumentException(nameof(firstname)))
            
        // check if last name is empty or null
        if String.IsNullOrWhiteSpace(lastname) then
            raise (ArgumentException(nameof(lastname)))
        
        let event = UserCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, email, firstname, lastname)
        let user = User()
        user.Apply(event)
        user
    
    member this.SubscribeToPlan planId customerId subscriptionId =
        let event = UserSubscribedToPlan(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, planId, customerId, subscriptionId)
        this.Apply(event)
        
    member this.Delete feedback =
        let event = UserDeleted(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, feedback)
        this.Apply(event)
        
    member this.ConnectToBrokerage accessToken refreshToken tokenType expiresInSeconds scope refreshTokenExpiresInSeconds =
        let event = UserConnectedToBrokerage(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, accessToken, refreshToken, tokenType, expiresInSeconds, scope, refreshTokenExpiresInSeconds)
        this.Apply(event)
        
    member this.DisconnectFromBrokerage() =
        let event = UserDisconnectedFromBrokerage(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow)
        this.Apply(event)
        
    member this.RefreshBrokerageConnection accessToken refreshToken tokenType expiresInSeconds scope refreshTokenExpiresInSeconds =
        let event = UserRefreshedBrokerageConnection(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, accessToken, refreshToken, tokenType, expiresInSeconds, scope, refreshTokenExpiresInSeconds)
        this.Apply(event)
        
    member this.PasswordHashMatches passwordHash =
        this.State.PasswordHashMatches(passwordHash)
        
    member this.Confirm() =
        match this.State.Verified.HasValue with
        | true -> ()
        | false ->
            let event = UserConfirmed(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow)
            this.Apply(event)
            
    member this.SetPassword hash salt =
        let event = UserPasswordSet(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, hash, salt)
        this.Apply(event)
        
    member this.RequestPasswordReset ``when`` =
        let event = UserPasswordResetRequested(Guid.NewGuid(), this.Id, ``when``)
        this.Apply(event)
        
    member this.SetSetting name value =
        let event = UserSettingSet(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, name, value)
        this.Apply(event)