namespace core.Account

open System
open System.Collections.Generic
open core.Shared
open Microsoft.FSharp.Core

type UserState() =
    let mutable _id = Guid.Empty
    let mutable _created = DateTimeOffset.MinValue
    let mutable _email = null
    let mutable _firstname = null
    let mutable _lastname = null
    let mutable _passwordHash = null
    let mutable _salt = null
    let mutable _deleted: DateTimeOffset option = None
    let mutable _deleteFeedback = null
    let mutable _verified: DateTimeOffset option = None
    let mutable _subscriptionLevel = "Free"
    let mutable _brokerageAccessToken = null
    let mutable _brokerageRefreshToken = null
    let mutable _brokerageAccessTokenExpires = DateTimeOffset.MinValue
    let mutable _brokerageRefreshTokenExpires = DateTimeOffset.MinValue
    let mutable _connectedToBrokerage = false
    let mutable _maxLoss: decimal option = None
    let _brokerageTransactionIdSet = HashSet<string>()
    let mutable _interestReceived = 0m
    let mutable _cashTransferred = 0m

    member _.Id with get() = _id
    member _.Created with get() = _created
    member _.Email with get() = _email
    member _.Firstname with get() = _firstname
    member _.Lastname with get() = _lastname
    member _.Deleted with get() = _deleted
    member _.DeleteFeedback with get() = _deleteFeedback
    member _.Verified with get() = _verified
    member _.SubscriptionLevel with get() = _subscriptionLevel
    member _.IsPasswordAvailable = _salt <> null
    member _.Name = sprintf "%s %s" _firstname _lastname
    member _.BrokerageAccessToken with get() = _brokerageAccessToken
    member _.BrokerageRefreshToken with get() = _brokerageRefreshToken
    member _.BrokerageAccessTokenExpires with get() = _brokerageAccessTokenExpires
    member _.BrokerageRefreshTokenExpires with get() = _brokerageRefreshTokenExpires
    member _.ConnectedToBrokerage with get() = _connectedToBrokerage
    member _.BrokerageAccessTokenExpired = _brokerageAccessTokenExpires < DateTimeOffset.UtcNow
    member _.MaxLoss with get() = _maxLoss
    member _.InterestReceived with get() = _interestReceived
    member _.CashTransferred with get() = _cashTransferred

    member internal this.ApplyInternal(c: UserCreated) =
        _id <- c.AggregateId
        _created <- c.When
        _email <- c.Email
        _firstname <- c.Firstname
        _lastname <- c.Lastname

    member this.ApplyInternal(p: UserPasswordSet) =
        _passwordHash <- p.Hash
        _salt <- p.Salt

    [<Obsolete("No longer tracking this")>]
    member internal this.ApplyInternal(l: UserLoggedIn) = ()

    member internal this.ApplyInternal(d: UserDeleted) =
        _deleted <- Some d.When
        _deleteFeedback <- d.Feedback

    member internal this.ApplyInternal(d: UserConfirmed) =
        _verified <- Some d.When

    member internal this.ApplyInternal(p: UserSubscribedToPlan) = ()

    member internal this.ApplyInternal(_: UserPasswordResetRequested) = ()

    member private this.RefreshBrokerageData(accessToken: string, refreshToken: string, eventTimestamp: DateTimeOffset) =
        _connectedToBrokerage <- true
        _brokerageAccessToken <- accessToken
        _brokerageRefreshToken <- refreshToken
        _brokerageAccessTokenExpires <- eventTimestamp.AddSeconds(1800.0)
        _brokerageRefreshTokenExpires <- eventTimestamp.AddDays(7.0)

    member internal this.ApplyInternal(e: UserConnectedToBrokerage) =
        this.RefreshBrokerageData(e.AccessToken, e.RefreshToken, e.When)

    member internal this.ApplyInternal(e: UserRefreshedBrokerageConnection) =
        this.RefreshBrokerageData(e.AccessToken, e.RefreshToken, e.When)

    member internal this.ApplyInternal(_: UserDisconnectedFromBrokerage) =
        _connectedToBrokerage <- false
        _brokerageAccessToken <- null
        _brokerageRefreshToken <- null
        _brokerageAccessTokenExpires <- DateTimeOffset.MinValue

    member private this.ApplyInternal(e: UserBrokerageInterestApplied) =
        _brokerageTransactionIdSet.Add(e.ActivityId) |> ignore
        _interestReceived <- _interestReceived + e.NetAmount

    member private this.ApplyInternal(e: UserCashTransferApplied) =
        _brokerageTransactionIdSet.Add(e.ActivityId) |> ignore
        _cashTransferred <- _cashTransferred + e.NetAmount

    member this.ContainsBrokerageTransaction(activityId: string) =
        _brokerageTransactionIdSet.Contains(activityId)

    member internal this.ApplyInternal(e: UserSettingSet) =
        match e.Key with
        | "maxLoss" ->
            let maxLoss = Decimal.Parse(e.Value)
            _maxLoss <- Some maxLoss
        | _ ->
            raise (InvalidOperationException(sprintf "Unknown setting: %s" e.Key))

    member this.PasswordHashMatches(hash: string) =
        _passwordHash = hash

    member this.GetSalt() = _salt

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? UserCreated as e -> this.ApplyInternal(e)
        | :? UserPasswordSet as e -> this.ApplyInternal(e)
        | :? UserLoggedIn as e -> this.ApplyInternal(e)
        | :? UserDeleted as e -> this.ApplyInternal(e)
        | :? UserConfirmed as e -> this.ApplyInternal(e)
        | :? UserSubscribedToPlan as e -> this.ApplyInternal(e)
        | :? UserPasswordResetRequested as e -> this.ApplyInternal(e)
        | :? UserConnectedToBrokerage as e -> this.ApplyInternal(e)
        | :? UserRefreshedBrokerageConnection as e -> this.ApplyInternal(e)
        | :? UserDisconnectedFromBrokerage as e -> this.ApplyInternal(e)
        | :? UserBrokerageInterestApplied as e -> this.ApplyInternal(e)
        | :? UserCashTransferApplied as e -> this.ApplyInternal(e)
        | :? UserSettingSet as e -> this.ApplyInternal(e)
        | _ -> ()

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)
