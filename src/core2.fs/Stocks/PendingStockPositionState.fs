namespace core.Stocks

open System
open core.Shared
open Microsoft.FSharp.Core

type PendingStockPositionState() =
    let mutable _id = Guid.Empty
    let mutable _ticker: Ticker = Unchecked.defaultof<Ticker>
    let mutable _userId = Guid.Empty
    let mutable _bid = 0m
    let mutable _price: decimal option = None
    let mutable _numberOfShares = 0m
    let mutable _stopPrice: decimal option = None
    let mutable _sizeStopPrice: decimal option = None
    let mutable _notes: string = null
    let mutable _strategy: string = null
    let mutable _created = DateTimeOffset.MinValue
    let mutable _closed: DateTimeOffset option = None
    let mutable _purchased = false
    let mutable _closeReason: string = null
    let mutable _orderDuration: string = null
    let mutable _orderType: string = null

    member _.Id = _id
    member _.Ticker = _ticker
    member _.UserId = _userId
    member _.Bid = _bid
    member _.Price = _price
    member _.NumberOfShares = _numberOfShares
    member _.StopPrice = _stopPrice
    member _.SizeStopPrice = _sizeStopPrice
    member _.Notes = _notes
    member _.Strategy = _strategy
    member _.Created = _created
    member _.Closed = _closed
    member this.IsClosed = _closed.IsSome
    member this.HasStopPrice = _stopPrice.IsSome
    member this.IsOpen = not this.IsClosed
    member _.Purchased = _purchased
    member _.CloseReason = _closeReason
    member this.NumberOfDaysActive = 
        int ((match _closed with Some c -> c | None -> DateTimeOffset.UtcNow) - _created).TotalDays
    member this.StopLossAmount = 
        match _stopPrice with
        | Some sp -> _numberOfShares * (sp - _bid)
        | None -> 0m
    member _.OrderDuration = _orderDuration
    member _.OrderType = _orderType

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? PendingStockPositionCreated as e -> this.ApplyInternal(e)
        | :? PendingStockPositionCreatedWithStrategy as e -> this.ApplyInternal(e)
        | :? PendingStockPositionCreatedWithStrategyAndSizeStop as e -> this.ApplyInternal(e)
        | :? PendingStockPositionOrderDetailsAdded as e -> this.ApplyInternal(e)
        | :? PendingStockPositionClosed as e -> this.ApplyInternal(e)
        | :? PendingStockPositionClosedWithReason as e -> this.ApplyInternal(e)
        | :? PendingStockPositionRealized as e -> this.ApplyInternal(e)
        | _ -> ()

    member private this.ApplyInternal(created: PendingStockPositionCreated) =
        this.ApplyInternal(
            PendingStockPositionCreatedWithStrategy(
                id = created.Id,
                aggregateId = created.AggregateId,
                ``when`` = created.When,
                notes = created.Notes,
                numberOfShares = created.NumberOfShares,
                price = created.Price,
                stopPrice = created.StopPrice,
                strategy = null,
                ticker = created.Ticker,
                userId = created.UserId
            )
        )

    member private this.ApplyInternal(created: PendingStockPositionCreatedWithStrategy) =
        this.ApplyInternal(
            PendingStockPositionCreatedWithStrategyAndSizeStop(
                id = created.Id,
                aggregateId = created.AggregateId,
                ``when`` = created.When,
                notes = created.Notes,
                numberOfShares = created.NumberOfShares,
                price = created.Price,
                stopPrice = created.StopPrice,
                sizeStopPrice = None,
                strategy = created.Strategy,
                ticker = created.Ticker,
                userId = created.UserId
            )
        )

    member private this.ApplyInternal(created: PendingStockPositionCreatedWithStrategyAndSizeStop) =
        _created <- created.When
        _id <- created.AggregateId
        _notes <- created.Notes
        _numberOfShares <- created.NumberOfShares
        _bid <- created.Price
        _stopPrice <- created.StopPrice
        _sizeStopPrice <- created.SizeStopPrice
        _strategy <- created.Strategy
        _ticker <- Ticker(created.Ticker)
        _userId <- created.UserId

    member private this.ApplyInternal(details: PendingStockPositionOrderDetailsAdded) =
        _orderType <- details.OrderType
        _orderDuration <- details.OrderDuration

    member private this.ApplyInternal(closed: PendingStockPositionClosed) =
        if closed.Price.IsSome then
            this.ApplyInternal(PendingStockPositionRealized(closed.Id, closed.AggregateId, closed.When, closed.Price.Value))
        else
            this.ApplyInternal(PendingStockPositionClosedWithReason(closed.Id, closed.AggregateId, closed.When, "Reason not provided"))

    member private this.ApplyInternal(closed: PendingStockPositionClosedWithReason) =
        _closed <- Some closed.When
        _closeReason <- closed.Reason

    member private this.ApplyInternal(realized: PendingStockPositionRealized) =
        _price <- Some realized.Price
        _purchased <- true
        _closed <- Some realized.When

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)
