namespace core.Cryptos

open System
open core.Shared

type ICryptoTransaction =
    abstract Id: Guid
    abstract When: DateTimeOffset
    abstract Quantity: decimal
    abstract DollarAmount: decimal
    abstract Token: string
    abstract Notes: string

[<AllowNullLiteral>]
type internal CryptoDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

type CryptoPurchased(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, token: string, quantity: decimal, dollarAmountSpent: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Token = token with get
    member val Quantity = quantity with get
    member val DollarAmountSpent = dollarAmountSpent with get
    member val Notes = notes with get
    member this.DollarAmount = this.DollarAmountSpent
    interface ICryptoTransaction with
        member this.Id = this.Id
        member this.When = this.When
        member this.Quantity = this.Quantity
        member this.DollarAmount = this.DollarAmount
        member this.Token = this.Token
        member this.Notes = this.Notes

type CryptoSold(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, token: string, quantity: decimal, dollarAmountReceived: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Token = token with get
    member val Quantity = quantity with get
    member val DollarAmountReceived = dollarAmountReceived with get
    member val Notes = notes with get
    member this.DollarAmount = this.DollarAmountReceived
    interface ICryptoTransaction with
        member this.Id = this.Id
        member this.When = this.When
        member this.Quantity = this.Quantity
        member this.DollarAmount = this.DollarAmount
        member this.Token = this.Token
        member this.Notes = this.Notes

type CryptoAwarded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, token: string, quantity: decimal, dollarAmountWorth: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Token = token with get
    member val Quantity = quantity with get
    member val DollarAmountWorth = dollarAmountWorth with get
    member val Notes = notes with get
    member this.DollarAmount = this.DollarAmountWorth
    interface ICryptoTransaction with
        member this.Id = this.Id
        member this.When = this.When
        member this.Quantity = this.Quantity
        member this.DollarAmount = this.DollarAmount
        member this.Token = this.Token
        member this.Notes = this.Notes

type CryptoYielded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, token: string, quantity: decimal, dollarAmountWorth: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Token = token with get
    member val Quantity = quantity with get
    member val DollarAmountWorth = dollarAmountWorth with get
    member val Notes = notes with get
    member this.DollarAmount = this.DollarAmountWorth
    interface ICryptoTransaction with
        member this.Id = this.Id
        member this.When = this.When
        member this.Quantity = this.Quantity
        member this.DollarAmount = this.DollarAmount
        member this.Token = this.Token
        member this.Notes = this.Notes

[<AllowNullLiteral>]
type internal CryptoTransactionDeleted(id: Guid, aggregateId: Guid, transactionId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val TransactionId = transactionId with get

[<AllowNullLiteral>]
type internal CryptoObtained(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, token: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Token = token with get
    member val UserId = userId with get
