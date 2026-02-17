namespace core.Stocks

open System
open System.Collections.Generic
open core.Shared

type PendingStockPosition =
    inherit Aggregate<PendingStockPositionState>

    new (events: IEnumerable<AggregateEvent>) = { inherit Aggregate<PendingStockPositionState>(events) }

    new (notes: string, numberOfShares: decimal, price: decimal, stopPrice: decimal, sizeStopPrice: decimal, strategy: string, ticker: Ticker, userId: Guid) as this =
        { inherit Aggregate<PendingStockPositionState>() }
        then
            if userId = Guid.Empty then
                raise (InvalidOperationException("Missing user id"))

            if price <= 0m then
                raise (InvalidOperationException("Price cannot be negative or zero"))

            if numberOfShares = 0m then
                raise (InvalidOperationException("Number of shares cannot be zero"))

            if stopPrice < 0m then
                raise (InvalidOperationException("Stop price cannot be negative or zero"))

            if sizeStopPrice < 0m then
                raise (InvalidOperationException("Size stop price cannot be negative or zero"))

            if String.IsNullOrWhiteSpace(notes) then
                raise (InvalidOperationException("Notes cannot be blank"))

            if String.IsNullOrWhiteSpace(strategy) then
                raise (InvalidOperationException("Strategy cannot be blank"))

            this.Apply(
                PendingStockPositionCreatedWithStrategyAndSizeStop(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    ``when`` = DateTimeOffset.UtcNow,
                    userId = userId,
                    ticker = ticker.Value,
                    price = price,
                    numberOfShares = numberOfShares,
                    stopPrice = Some stopPrice,
                    sizeStopPrice = Some sizeStopPrice,
                    notes = notes,
                    strategy = strategy
                )
            )

    member this.Purchase(price: decimal) =
        if price <= 0m then
            raise (InvalidOperationException("Price cannot be negative or zero"))

        this.Apply(
            PendingStockPositionRealized(
                Guid.NewGuid(),
                this.State.Id,
                ``when`` = DateTimeOffset.UtcNow,
                price = price
            )
        )

    member this.AddOrderDetails(orderType: string, orderDuration: string) =
        if String.IsNullOrWhiteSpace(orderType) then
            raise (InvalidOperationException("Order type cannot be blank"))

        if String.IsNullOrWhiteSpace(orderDuration) then
            raise (InvalidOperationException("Order duration cannot be blank"))

        this.Apply(
            PendingStockPositionOrderDetailsAdded(
                Guid.NewGuid(),
                this.State.Id,
                ``when`` = DateTimeOffset.UtcNow,
                orderType = orderType,
                orderDuration = orderDuration
            )
        )

    member this.Close(reason: string) =
        if this.State.IsClosed then
            ()
        elif String.IsNullOrWhiteSpace(reason) then
            raise (InvalidOperationException("Reason cannot be blank"))
        else
            this.Apply(
                PendingStockPositionClosedWithReason(
                    Guid.NewGuid(),
                    this.State.Id,
                    ``when`` = DateTimeOffset.UtcNow,
                    reason = reason
                )
            )
