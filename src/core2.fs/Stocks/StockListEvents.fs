namespace core.Stocks

open System
open core.Shared

[<AllowNullLiteral>]
type internal StockListCreated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, description: string, name: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Description = description with get
    member val Name = name with get
    member val UserId = userId with get

[<AllowNullLiteral>]
type internal StockListUpdated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, description: string, name: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Description = description with get
    member val Name = name with get

[<AllowNullLiteral>]
type internal StockListTickerAdded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, note: string, ticker: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Note = note with get
    member val Ticker = ticker with get

[<AllowNullLiteral>]
type internal StockListTickerRemoved(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, ticker: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Ticker = ticker with get

[<AllowNullLiteral>]
type internal StockListTagAdded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, tag: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Tag = tag with get

[<AllowNullLiteral>]
type internal StockListTagRemoved(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, tag: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Tag = tag with get

[<AllowNullLiteral>]
type internal StockListCleared(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    inherit AggregateEvent(id, aggregateId, ``when``)
