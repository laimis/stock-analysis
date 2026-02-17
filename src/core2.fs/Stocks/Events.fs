namespace core.Stocks

open System
open core.Shared

type IStockTransaction =
    abstract NumberOfShares: decimal
    abstract Price: decimal
    abstract Id: Guid
    abstract When: DateTimeOffset
    abstract Notes: string

type IStockTransactionWithStopPrice =
    inherit IStockTransaction
    abstract StopPrice: decimal option

[<AllowNullLiteral>]
type internal StockDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)

type StockPurchased(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, numberOfShares: decimal, price: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val NumberOfShares = numberOfShares with get
    member val Price = price with get
    member val Notes = notes with get
    interface IStockTransaction with
        member this.NumberOfShares = this.NumberOfShares
        member this.Price = this.Price
        member this.Id = this.Id
        member this.When = this.When
        member this.Notes = this.Notes

type StockPurchased_v2(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, numberOfShares: decimal, price: decimal, notes: string, stopPrice: decimal option) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val NumberOfShares = numberOfShares with get
    member val Price = price with get
    member val Notes = notes with get
    member val StopPrice = stopPrice with get
    interface IStockTransactionWithStopPrice with
        member this.NumberOfShares = this.NumberOfShares
        member this.Price = this.Price
        member this.Id = this.Id
        member this.When = this.When
        member this.Notes = this.Notes
        member this.StopPrice = this.StopPrice

type StockSold(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, numberOfShares: decimal, price: decimal, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val NumberOfShares = numberOfShares with get
    member val Price = price with get
    member val Notes = notes with get
    interface IStockTransaction with
        member this.NumberOfShares = this.NumberOfShares
        member this.Price = this.Price
        member this.Id = this.Id
        member this.When = this.When
        member this.Notes = this.Notes

[<AllowNullLiteral>]
type internal StockTransactionDeleted(id: Guid, aggregateId: Guid, transactionId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val TransactionId = transactionId with get

[<Obsolete("Use position labels instead")>]
[<AllowNullLiteral>]
type internal StockCategoryChanged(id: Guid, aggregateId: Guid, category: string, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Category = category with get

[<AllowNullLiteral>]
type internal TickerObtained(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, ticker: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Ticker = ticker with get
    member val UserId = userId with get

[<AllowNullLiteral>]
type StopPriceSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, stopPrice: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val StopPrice = stopPrice with get

[<AllowNullLiteral>]
type NotesAdded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, positionId: int, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val PositionId = positionId with get
    member val Notes = notes with get

[<AllowNullLiteral>]
type internal RiskAmountSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, riskAmount: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val RiskAmount = riskAmount with get

[<AllowNullLiteral>]
type StopDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get

[<AllowNullLiteral>]
type TradeGradeAssigned(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, grade: string, note: string, positionId: int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val PositionId = positionId with get
    member val Grade = grade with get
    member val Note = note with get

[<AllowNullLiteral>]
type PositionDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, positionId: int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val PositionId = positionId with get
    member val UserId = userId with get

[<AllowNullLiteral>]
type PositionLabelSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, positionId: int, key: string, value: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val PositionId = positionId with get
    member val Key = key with get
    member val Value = value with get

[<AllowNullLiteral>]
type PositionLabelDeleted(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, positionId: int, key: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val PositionId = positionId with get
    member val Key = key with get

[<AllowNullLiteral>]
type PositionRiskAmountSet(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, positionId: int, riskAmount: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val PositionId = positionId with get
    member val RiskAmount = riskAmount with get
