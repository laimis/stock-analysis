namespace core.Account

open System
open core.Shared

[<AllowNullLiteral>]
type UserConfirmed(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

[<AllowNullLiteral>]
type UserCreated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, email: string, firstname: string, lastname: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Email = email with get
    member val Firstname = firstname with get
    member val Lastname = lastname with get

[<AllowNullLiteral>]
type UserDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, feedback: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Feedback = feedback with get

[<Obsolete("dropped this functionality from domain")>]
[<AllowNullLiteral>]
type internal UserLoggedIn(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

[<AllowNullLiteral>]
type UserPasswordResetRequested(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

[<AllowNullLiteral>]
type UserPasswordSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, hash: string, salt: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Hash = hash with get
    member val Salt = salt with get

[<AllowNullLiteral>]
type UserSubscribedToPlan(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, planId: string, customerId: string, subscriptionId: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val PlanId = planId with get
    member val CustomerId = customerId with get
    member val SubscriptionId = subscriptionId with get

[<AllowNullLiteral>]
type UserConnectedToBrokerage(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, accessToken: string, refreshToken: string, tokenType: string, expiresInSeconds: int64, scope: string, refreshTokenExpiresInSeconds: int64) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val AccessToken = accessToken with get
    member val RefreshToken = refreshToken with get
    member val TokenType = tokenType with get
    member val ExpiresInSeconds = expiresInSeconds with get
    member val Scope = scope with get
    [<Obsolete("this will not have a valid value and is here to preserve the event structure")>]
    member val RefreshTokenExpiresInSeconds = refreshTokenExpiresInSeconds with get

[<AllowNullLiteral>]
type UserRefreshedBrokerageConnection(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, accessToken: string, refreshToken: string, tokenType: string, expiresInSeconds: int64, scope: string, refreshTokenExpiresInSeconds: int64) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val AccessToken = accessToken with get
    member val RefreshToken = refreshToken with get
    member val TokenType = tokenType with get
    member val ExpiresInSeconds = expiresInSeconds with get
    member val Scope = scope with get
    member val RefreshTokenExpiresInSeconds = refreshTokenExpiresInSeconds with get

[<AllowNullLiteral>]
type UserDisconnectedFromBrokerage(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

[<AllowNullLiteral>]
type UserSettingSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, key: string, value: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Key = key with get
    member val Value = value with get

[<AllowNullLiteral>]
type UserBrokerageInterestApplied(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, activityId: string, netAmount: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val ActivityId = activityId with get
    member val NetAmount = netAmount with get

[<AllowNullLiteral>]
type UserCashTransferApplied(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, activityId: string, netAmount: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val ActivityId = activityId with get
    member val NetAmount = netAmount with get
