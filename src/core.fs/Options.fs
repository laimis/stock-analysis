namespace core.fs.Options

open System
open Microsoft.Win32.SafeHandles
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
    OptionType: OptionType
    StrikePrice: decimal
    ExpirationDate: DateTimeOffset
    Quantity: decimal
    Price: decimal
}

type OptionTransaction = {
    Expiration: string
    Strike: decimal
    OptionType: string
    Quantity: decimal
    Price: decimal
}

type OptionPositionState =
    {
        PositionId: OptionPositionId
        UnderlyingTicker : Ticker
        Legs: Map<string, OptionLeg>
        Opened: DateTimeOffset
        Closed: DateTimeOffset option
        Transactions: OptionTransaction list
        
        Version: int
        Events: AggregateEvent list
    }
    
    member this.IsOpen = this.Closed.IsNone
    member this.IsClosed = this.Closed.IsSome
    member this.TotalCost = this.Transactions |> List.sumBy (fun x -> x.Price * x.Quantity)
    
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
        
        
type OptionPositionOpened(id, aggregateId, ``when``, underlyingTicker:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.PositionId = aggregateId |> OptionPositionId
    member this.UnderlyingTicker = underlyingTicker

// create abstract class OptionContractTransaction
type OptionContractTransaction(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:decimal, price:decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    member this.Price = price

type OptionContractPurchased(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:decimal, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price)
    
type OptionContractSold(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:decimal, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, -quantity, price)
    
type OptionPositionClosed(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
type OptionContractsExpired(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity

module OptionPosition =
    
    // ensure that the option is stored as yyyy-MM-dd, and can be converted to datetime offset
    let toOptionExpirationDate (date:string) =
        match DateTimeOffset.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None) with 
        | true, d -> d
        | _ -> failwith "Invalid date format. Expected yyyy-MM-dd"
        
        
    let createInitialState (event: OptionPositionOpened) : OptionPositionState=
        {
            PositionId = event.PositionId
            Closed = None
            Opened = event.When
            Transactions = []
            UnderlyingTicker = event.UnderlyingTicker |> Ticker
            Legs = Map<string, OptionLeg>
            Version = 1
            Events = [event]
        }
        
    let private apply (event: AggregateEvent) p =
        
        match event with
        
        | :? OptionPositionOpened as _ ->
            p // pass through, this should be done by createInitialState
            
        | :? OptionContractPurchased as x ->
            let newTransactions = p.Transactions @ [{ Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = x.Quantity; Price = x.Price; }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractSold as x ->
            let newTransactions = p.Transactions @ [{ Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = x.Quantity; Price = x.Price; }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionPositionClosed as x ->
            { p with Closed = Some x.When; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractsExpired as x ->
            { p with Version = p.Version + 1; Events = p.Events @ [x] }
            
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

    let private verifyOptionTransactionParams (expiration:string) (strike:decimal) (quantity:decimal) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        date |> failIfInvalidDate
        let expirationAsDate = expiration |> toOptionExpirationDate 
        
        if expirationAsDate < date then
            failwith "Expiration date must be after transaction date"
            
        if quantity <= 0m then
            failwith "Quantity must be greater than zero"
            
        if price <= 0m then
            failwith "Price must be greater than zero"
            
        if strike <= 0m then
            failwith "Strike price must be greater than zero"
            
        if position.IsClosed then
            failwith "Position is closed"
            
    let private closeIfAllContractsAreClosed date (position:OptionPositionState) =
        if position.IsClosed then
            failwith "Position is already closed"
            
        if position.Transactions |> List.sumBy (fun x -> x.Quantity) = 0m then
            let e = OptionPositionClosed(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date)
            apply e position
        else
            position
        
    let buyToOpen (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:decimal) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractPurchased(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position
        
    let buyToClose (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:decimal) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractPurchased(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position |> closeIfAllContractsAreClosed date
        
    let sellToOpen (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:decimal) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractSold(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position
        
    let sellToClose (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:decimal) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractSold(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position |> closeIfAllContractsAreClosed date

    let expire (expiration:string) (strike:decimal) (optionType:OptionType) (position:OptionPositionState) =
        
        if position.IsClosed then
            failwith "Position is closed"
            
        // find all contracts with the same expiration and option type
        let contractsToExpire =
            position.Transactions
            |> List.filter (fun x -> x.Expiration = expiration && x.OptionType = optionType.ToString() && x.Strike = strike)
            |> List.map (fun x -> x.Quantity)
            |> List.sum
            
        if contractsToExpire = 0m then
            let e = OptionContractsExpired(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, DateTimeOffset.UtcNow, expiration, strike, optionType.ToString(), contractsToExpire)
            apply e position
        else
            position
