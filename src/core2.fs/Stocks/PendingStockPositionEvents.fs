namespace core.Stocks

open System
open core.Shared

[<AllowNullLiteral>]
type internal PendingStockPositionCreated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, userId: Guid, ticker: string, price: decimal, numberOfShares: decimal, stopPrice: decimal option, notes: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val Price = price with get
    member val NumberOfShares = numberOfShares with get
    member val StopPrice = stopPrice with get
    member val Notes = notes with get

[<AllowNullLiteral>]
type internal PendingStockPositionCreatedWithStrategy(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, notes: string, numberOfShares: decimal, price: decimal, stopPrice: decimal option, strategy: string, ticker: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val Price = price with get
    member val NumberOfShares = numberOfShares with get
    member val StopPrice = stopPrice with get
    member val Notes = notes with get
    member val Strategy = strategy with get

[<AllowNullLiteral>]
type internal PendingStockPositionCreatedWithStrategyAndSizeStop(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, notes: string, numberOfShares: decimal, price: decimal, stopPrice: decimal option, sizeStopPrice: decimal option, strategy: string, ticker: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val UserId = userId with get
    member val Ticker = ticker with get
    member val Price = price with get
    member val NumberOfShares = numberOfShares with get
    member val StopPrice = stopPrice with get
    member val SizeStopPrice = sizeStopPrice with get
    member val Notes = notes with get
    member val Strategy = strategy with get

[<AllowNullLiteral>]
type PendingStockPositionRealized(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, price: decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Price = price with get

[<AllowNullLiteral>]
type PendingStockPositionOrderDetailsAdded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, orderType: string, orderDuration: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val OrderType = orderType with get
    member val OrderDuration = orderDuration with get

[<AllowNullLiteral>]
type PendingStockPositionClosed(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, purchased: bool, price: decimal option) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Purchased = purchased with get
    member val Price = price with get

[<AllowNullLiteral>]
type PendingStockPositionClosedWithReason(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, reason: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Reason = reason with get
