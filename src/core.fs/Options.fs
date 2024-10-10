namespace core.fs.Options

open System
open core.Shared

type OptionPositionId = OptionPositionId of Guid

module OptionPositionId =
    let create() = OptionPositionId (Guid.NewGuid())
    let guid (OptionPositionId id) = id
    let parse (id:string) = id |> Guid.Parse |> OptionPositionId
    
type OptionTicker = OptionTicker of string

module OptionTicker =
    let create (ticker:string) =
        if String.IsNullOrWhiteSpace ticker then
            raise (ArgumentException(nameof ticker))
        ticker.ToUpper() |> OptionTicker
    let ticker (OptionTicker ticker) = ticker
    
type OptionType =
    | Call
    | Put
    
    with
        override this.ToString() =
            match this with
            | Call -> nameof Call
            | Put -> nameof Put
            
        static member FromString(value:string) =
            match value with
            | nameof Call -> Call
            | nameof Put -> Put
            | "CALL" -> Call
            | "PUT" -> Put
            | _ -> failwithf $"Invalid option type: %s{value}"
            

type OptionLeg = {
    LegId: int
    OptionType: OptionType
    StrikePrice: decimal
    ExpirationDate: DateTimeOffset
    Quantity: int
    Price: decimal
}

type OptionPositionState =
    {
        PositionId: OptionPositionId
        UnderlyingTicker : Ticker
        Opened: DateTimeOffset
        Closed: DateTimeOffset option
        Legs: OptionLeg list
        
        Version: int
        Events: AggregateEvent list
    }
    
    member this.IsOpen = this.Closed.IsNone
    member this.IsClosed = this.Closed.IsSome
    
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
        
        
type OptionPositionOpened(id, aggregateId, ``when``, underlyingTicker:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.PositionId = aggregateId |> OptionPositionId
    member this.UnderlyingTicker = underlyingTicker
    
type OptionLegPurchased(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    member this.Price = price

module OptionPosition =
        
    let createInitialState (event: OptionPositionOpened) : OptionPositionState=
        {
            PositionId = event.PositionId
            Closed = None
            Opened = event.When
            Legs = []
            UnderlyingTicker = event.UnderlyingTicker |> Ticker 
            Version = 1
            Events = [event]
        }
        
    let private apply (event: AggregateEvent) p =
        
        match event with
        
        | :? OptionPositionOpened as _ ->
            
            p // pass through, this should be done by createInitialState
            
        // | :? StockPositionStopSet as x ->
        //     let newTransactions = p.Transactions @ [Stop { TransactionId = x.Id; StopPrice = Some x.StopPrice; Date = x.When }]
        //     { p with Transactions = newTransactions; StopPrice = Some x.StopPrice; Version = p.Version + 1; Events = p.Events @ [x] }
        //
        | :? OptionLegPurchased as x ->
            let newLegs = p.Legs @ [{ LegId = p.Legs.Length + 1; OptionType = x.OptionType; StrikePrice = x.Strike; ExpirationDate = x.Expiration; Quantity = x.Quantity; Price = x.Price }]
            { p with Legs = newLegs; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | _ -> failwith ("Unknown event: " + event.GetType().Name)

    let createFromEvents (events: AggregateEvent seq) =
        // build state from the first event, casting it to PositionOpened
        let state = events |> Seq.head :?> OptionPositionOpened |> createInitialState
        events |> Seq.skip 1 |> Seq.fold (fun acc e -> apply e acc) state

    let failIfInvalidDate date =
        if date < DateTimeOffset.UnixEpoch then
            failwith ("Date after " + DateTimeOffset.UnixEpoch.ToString() + " is required")
            
        if date.Subtract(DateTimeOffset.UtcNow).TotalHours >= 12 then
            failwith "Date cannot be in the future"
            
    let ``open`` (ticker:Ticker) date =
        date |> failIfInvalidDate
        
        OptionPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker.Value)
        |> createInitialState

    let buyLeg expiration strike optionType quantity price date (position:OptionPositionState) =
        
        date |> failIfInvalidDate
        expiration |> failIfInvalidDate
        
        if expiration < date then
            failwith "Expiration date must be after transaction date"
            
        if quantity <= 0 then
            failwith "Quantity must be greater than zero"
            
        if price <= 0 then
            failwith "Price must be greater than zero"
            
        if strike <= 0 then
            failwith "Strike price must be greater than zero"
            
        if position.IsClosed then
            failwith "Position is closed"
        
        let e = OptionLegPurchased(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType, quantity, price)
        
        apply e position
