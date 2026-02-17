namespace core.Cryptos

open System

[<AllowNullLiteral>]
type PositionInstance(token: string) =
    let mutable _firstOpen: DateTimeOffset option = None
    let mutable _quantity = 0m
    let mutable _cost = 0m
    let mutable _return = 0m
    let mutable _closed: DateTimeOffset option = None

    member this.DaysHeld =
        match _firstOpen with
        | Some fo -> int ((match _closed with Some c -> c | None -> DateTimeOffset.UtcNow).Subtract(fo).TotalDays)
        | None -> 0

    member _.Cost = _cost
    member _.Return = _return
    member this.Percentage = if _cost = 0m then 0m else Math.Round((_return - _cost) / _cost, 4)
    member this.Profit = _return - _cost
    member _.IsClosed = _closed.IsSome
    member _.Token = token
    member _.Closed = _closed

    member this.Buy(quantity: decimal, dollarAmountSpent: decimal, ``when``: DateTimeOffset) =
        if _quantity = 0m then
            _firstOpen <- Some ``when``
        _quantity <- _quantity + quantity
        _cost <- _cost + dollarAmountSpent

    member this.Sell(quantity: decimal, dollarAmountReceived: decimal, ``when``: DateTimeOffset) =
        _quantity <- _quantity - quantity
        if _quantity < 0m then
            raise (InvalidOperationException("Transaction would make amount owned invalid"))
        if _quantity = 0m then
            _closed <- Some ``when``
        _return <- _return + dollarAmountReceived
