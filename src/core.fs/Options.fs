namespace core.fs.Options

open System
open System.Collections.Generic
open core.Shared

type OptionPositionId = OptionPositionId of Guid
module OptionPositionId =
    let create() = OptionPositionId (Guid.NewGuid())
    let guid (OptionPositionId id) = id
    let parse (id:string) = id |> Guid.Parse |> OptionPositionId
    
type OptionExpiration =
    OptionExpiration of string
        with
            member this.ToDateTimeOffset() =
                DateTimeOffset.ParseExact(this.ToString(), "yyyy-MM-dd", null)
            override this.ToString() =
                match this with
                | OptionExpiration date -> date
                
            static member create(date:string) = OptionExpiration date
            static member createFromDateTimeOffset(date:DateTimeOffset) = date.ToString("yyyy-MM-dd") |> OptionExpiration 


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


type OptionTransaction = {
    EventId: Guid
    Expiration: string
    Strike: decimal
    OptionType: string
    Quantity: int
    Debited: decimal option
    Credited: decimal option
    When: DateTimeOffset
}

type OptionContract = {
    Expiration: OptionExpiration
    Strike: decimal
    OptionType: OptionType
}

type QuantityAndCost = QuantityAndCost of int * decimal

type OptionPositionState =
    {
        PositionId: OptionPositionId
        UnderlyingTicker : Ticker
        Opened: DateTimeOffset
        Closed: DateTimeOffset option
        Cost: decimal
        Profit: decimal
        Transactions: OptionTransaction list
        Contracts: Map<OptionContract, QuantityAndCost>
        Notes: core.fs.Note list
        Labels: Dictionary<string, string>
        Version: int
        Events: AggregateEvent list
    }
    member this.IsOpen = this.Closed.IsNone
    member this.IsClosed = this.Closed.IsSome
    member this.DaysHeld = 
        match this.Closed with
        | Some closed -> closed.Subtract(this.Opened).Days |> int
        | None -> DateTimeOffset.UtcNow.Subtract(this.Opened).Days |> int
    
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
        
        
type OptionPositionOpened(id, aggregateId, ``when``, underlyingTicker:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.PositionId = aggregateId |> OptionPositionId
    member this.UnderlyingTicker = underlyingTicker

// create abstract class OptionContractTransaction
type OptionContractTransaction(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int, price:decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    member this.Price = price

type OptionContractBoughtToOpen(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price)

type OptionContractBoughtToClose(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price)
    
type OptionContractSoldToOpen(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price)

type OptionContractSoldToClose(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int, price:decimal) =
    inherit OptionContractTransaction(id, aggregateId, ``when``, expiration, strike, optionType, quantity, price)
   
type OptionPositionClosed(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
type OptionContractsExpired(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    
type OptionContractsAssigned(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    
type OptionContractsExercised(id, aggregateId, ``when``, expiration:string, strike:decimal, optionType:string, quantity:int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.Expiration = expiration
    member this.Strike = strike
    member this.OptionType = optionType
    member this.Quantity = quantity
    
type OptionPositionDeleted(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
type OptionPositionNotesAdded(id, aggregateId, ``when``, content:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Content = content
    
type OptionPositionLabelSet(id, aggregateId, ``when``, key:string, value:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Key = key
    member this.Value = value
    
type OptionPositionLabelDeleted(id, aggregateId, ``when``, key:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Key = key

module OptionPosition =
    
    // ensure that the option is stored as yyyy-MM-dd, and can be converted to datetime offset
    let private toOptionExpirationDate (date:string) =
        match DateTimeOffset.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None) with 
        | true, d -> d
        | _ -> failwith "Invalid date format. Expected yyyy-MM-dd"
        
    let private createInitialState (event: OptionPositionOpened) : OptionPositionState=
        {
            PositionId = event.PositionId
            Closed = None
            Opened = event.When
            UnderlyingTicker = event.UnderlyingTicker |> Ticker
            Profit = 0m
            Cost = 0m
            Version = 1
            Notes = []
            Labels = Dictionary<string, string>()
            Transactions = []
            Contracts = Map.empty
            Events = [event]
        }
        
    let private apply (event: AggregateEvent) p =
        
        match event with
        
        | :? OptionPositionOpened as _ ->
            p // pass through, this should be done by createInitialState
            
        | :? OptionPositionNotesAdded as x ->
            { p with Notes = p.Notes @ [{created = x.When; content = x.Content; id = x.Id }]; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractBoughtToOpen as x ->
            let debit = decimal x.Quantity * x.Price
            let transaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = x.Quantity; Debited = debit |> Some; Credited = None; When = x.When }
            let contract = { Expiration = OptionExpiration.create(x.Expiration); Strike = x.Strike; OptionType = x.OptionType |> OptionType.FromString; }
            let (QuantityAndCost(quantity, cost)) = p.Contracts |> Map.tryFind contract |> Option.defaultValue (QuantityAndCost(0, 0m))
            let updatedQuantityAndCost = QuantityAndCost(quantity + x.Quantity, cost + debit)
            let updatedContracts = p.Contracts |> Map.add contract updatedQuantityAndCost
            { p with Transactions = p.Transactions @ [transaction]; Cost = p.Cost + debit; Version = p.Version + 1; Contracts = updatedContracts; Events = p.Events @ [x] }
            
        | :? OptionContractSoldToOpen as x ->
            let credit = decimal x.Quantity * x.Price
            let transaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = -1 * x.Quantity; Credited = credit |> Some; Debited = None; When = x.When }
            let contract = { Expiration = OptionExpiration.create(x.Expiration); Strike = x.Strike; OptionType = x.OptionType |> OptionType.FromString }
            let (QuantityAndCost(quantity, cost)) = p.Contracts |> Map.tryFind contract |> Option.defaultValue (QuantityAndCost(0, 0m))
            let updatedQuantityAndCost = QuantityAndCost(quantity - x.Quantity, cost + credit)
            let updatedContracts = p.Contracts |> Map.add contract updatedQuantityAndCost
            { p with Transactions = p.Transactions @ [transaction]; Cost = p.Cost - credit; Version = p.Version + 1; Contracts = updatedContracts; Events = p.Events @ [x] }
            
        | :? OptionContractBoughtToClose as x ->
            let debit = decimal x.Quantity * x.Price
            let transaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = x.Quantity; Debited = debit |> Some; Credited = None; When = x.When }
            let contract = { Expiration = OptionExpiration.create(x.Expiration); Strike = x.Strike; OptionType = x.OptionType |> OptionType.FromString }
            let (QuantityAndCost(quantity, cost)) = p.Contracts |> Map.tryFind contract |> Option.defaultValue (QuantityAndCost(0, 0m))
            let updatedQuantityAndCost = QuantityAndCost(quantity + x.Quantity, cost - debit)
            let updatedContracts = p.Contracts |> Map.add contract updatedQuantityAndCost
            { p with Transactions = p.Transactions @ [transaction]; Version = p.Version + 1; Contracts = updatedContracts; Events = p.Events @ [x] }
            
        | :? OptionContractSoldToClose as x ->
            let credit = decimal x.Quantity * x.Price
            let transaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = -1 * x.Quantity; Credited = credit |> Some; Debited = None; When = x.When }
            let contract = { Expiration = OptionExpiration.create(x.Expiration); Strike = x.Strike; OptionType = x.OptionType |> OptionType.FromString }
            let (QuantityAndCost(quantity, cost)) = p.Contracts |> Map.tryFind contract |> Option.defaultValue (QuantityAndCost(0, 0m))
            let updatedQuantityAndCost = QuantityAndCost(quantity - x.Quantity, cost - credit)
            let updatedContracts = p.Contracts |> Map.add contract updatedQuantityAndCost
            { p with Transactions = p.Transactions @ [transaction]; Version = p.Version + 1; Contracts = updatedContracts; Events = p.Events @ [x] }
            
        | :? OptionPositionClosed as x ->
            let debits = p.Transactions |> List.map _.Debited |> List.choose id |> List.sum
            let credits = p.Transactions |> List.map _.Credited |> List.choose id |> List.sum
            let profit = credits - debits
            { p with Closed = Some x.When; Profit = profit; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractsExpired as x ->
            let expirationTransaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = -1 * x.Quantity; Debited = None; Credited = None; When = x.When }
            { p with Transactions = p.Transactions @ [expirationTransaction]; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractsAssigned as x ->
            let assignmentTransaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = -1 * x.Quantity; Debited = None; Credited = None; When = x.When }
            { p with Transactions = p.Transactions @ [assignmentTransaction]; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionContractsExercised as x ->
            let exerciseTransaction = { EventId = x.Id; Expiration = x.Expiration; Strike = x.Strike; OptionType = x.OptionType; Quantity = -1 * x.Quantity; Debited = None; Credited = None; When = x.When }
            { p with Transactions = p.Transactions @ [exerciseTransaction]; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionPositionDeleted as x ->
            { p with Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionPositionLabelSet as x ->
            p.Labels[x.Key] <- x.Value
            { p with Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? OptionPositionLabelDeleted as x ->
            p.Labels.Remove(x.Key) |> ignore
            { p with Version = p.Version + 1; Events = p.Events @ [x] }
            
        | _ -> failwith ("Unknown event: " + event.GetType().Name)

    let createFromEvents (events: AggregateEvent seq) =
        // build state from the first event, casting it to PositionOpened
        let state = events |> Seq.head :?> OptionPositionOpened |> createInitialState
        events |> Seq.skip 1 |> Seq.fold (fun acc e -> apply e acc) state

    let private failIfInvalidDate date =
        if date < DateTimeOffset.UnixEpoch then
            failwith ("Date after " + DateTimeOffset.UnixEpoch.ToString() + " is required")
            
        if date.Subtract(DateTimeOffset.UtcNow).TotalHours >= 12 then
            failwith "Date cannot be in the future"
            
    let private applyNotesIfApplicable notes date position =
        match notes with
            | None -> position
            | Some notes when String.IsNullOrWhiteSpace(notes) -> position
            | Some notes when position.Notes |> List.exists (fun n -> n.content = notes) -> position
            | Some notes ->
                let e = OptionPositionNotesAdded(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, notes)
                apply e position
            
    let ``open`` (ticker:Ticker) date =
        date |> failIfInvalidDate
        
        OptionPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker.Value)
        |> createInitialState

    let private verifyOptionTransactionParams (expiration:string) (strike:decimal) (quantity:int) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        date |> failIfInvalidDate
        let expirationAsDate = expiration |> toOptionExpirationDate 
        
        if expirationAsDate < date then
            failwith "Expiration date must be after transaction date"
            
        if quantity <= 0 then
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
            
        if position.Transactions |> List.sumBy _.Quantity = 0 then
            let e = OptionPositionClosed(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date)
            apply e position
        else
            position
            
    let private findMatchingTransactions (expiration:string) (strike:decimal) (optionType:OptionType) (position:OptionPositionState) =
        position.Transactions
        |> List.filter (
            fun x ->
                x.Expiration = expiration &&
                x.OptionType = optionType.ToString() &&
                x.Strike = strike
        )
    
    let buyToOpen (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:int) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractBoughtToOpen(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position
        
    let sellToOpen (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:int) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        let e = OptionContractSoldToOpen(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position
        
    let buyToClose (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:int) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        // make sure that number of contracts available is greater than the quantity
        let contractsAvailable =
            position
            |> findMatchingTransactions expiration strike optionType
            |> List.map (fun x -> x.Quantity)
            |> List.sum
        
        if contractsAvailable > 0 then
            raise (InvalidOperationException($"Cannot buy back contracts that were not sold, have {contractsAvailable} and trying to buy {quantity} {strike} {optionType}"))
            
        if abs(contractsAvailable) < quantity then
            raise (InvalidOperationException($"Not enough contracts available to close, have {contractsAvailable} and trying to close {quantity}"))
        
        let e = OptionContractBoughtToClose(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position |> closeIfAllContractsAreClosed date
        
    let sellToClose (expiration:string) (strike:decimal) (optionType:OptionType) (quantity:int) (price:decimal) (date:DateTimeOffset) (position:OptionPositionState) =
        
        verifyOptionTransactionParams expiration strike quantity price date position
        
        // make sure that number of contracts available is greater than the quantity
        let contractsAvailable =
            position
            |> findMatchingTransactions expiration strike optionType
            |> List.map _.Quantity
            |> List.sum
            
        if contractsAvailable < quantity then
            raise (InvalidOperationException($"Not enough contracts available to close, have {contractsAvailable} and trying to close {quantity}"))
        
        let e = OptionContractSoldToClose(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, expiration, strike, optionType.ToString(), quantity, price)
        
        apply e position |> closeIfAllContractsAreClosed date

    let expire (expiration:string) (strike:decimal) (optionType:OptionType) (position:OptionPositionState) =
        
        if position.IsClosed then
            failwith "Position is closed"
            
        // find all contracts with the same expiration and option type
        let contractsToExpire =
            position
            |> findMatchingTransactions expiration strike optionType
            |> List.map (fun x -> x.Quantity)
            |> List.sum
            
        if contractsToExpire <> 0 then
            let e = OptionContractsExpired(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, DateTimeOffset.UtcNow, expiration, strike, optionType.ToString(), contractsToExpire)
            apply e position |> closeIfAllContractsAreClosed DateTimeOffset.UtcNow
        else
            raise (InvalidOperationException("No contracts to expire"))
            
    let assign (expiration:string) (strike:decimal) (optionType:OptionType) (position:OptionPositionState) =
        
        if position.IsClosed then
            failwith "Position is closed"
            
        // find all contracts with the same expiration and option type
        let contractsToAssign =
            position
            |> findMatchingTransactions expiration strike optionType
            |> List.map (fun x -> x.Quantity)
            |> List.sum
        
        if contractsToAssign > 0 then
            raise (InvalidOperationException("Cannot assign contracts that are not owned"))
        
        if contractsToAssign < 0 then // can only assign contracts that are owned
            let e = OptionContractsAssigned(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, DateTimeOffset.UtcNow, expiration, strike, optionType.ToString(), contractsToAssign)
            apply e position |> closeIfAllContractsAreClosed DateTimeOffset.UtcNow
        else
            raise (InvalidOperationException("No contracts to assign"))
            
    let exercise (expiration:string) (strike:decimal) (optionType:OptionType) (position:OptionPositionState) =
        
        if position.IsClosed then
            failwith "Position is closed"
            
        // find all contracts with the same expiration and option type
        let contractsToExercise =
            position
            |> findMatchingTransactions expiration strike optionType
            |> List.map (fun x -> x.Quantity)
            |> List.sum
            
        if contractsToExercise <> 0 then
            let e = OptionContractsExercised(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, DateTimeOffset.UtcNow, expiration, strike, optionType.ToString(), contractsToExercise)
            apply e position
        else
            position
            
    let delete position =
        let e = OptionPositionDeleted(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, DateTimeOffset.UtcNow)
        apply e position
        
    let addNotes = applyNotesIfApplicable
    
    let setLabel key value date position =
        
        match key with
        | x when String.IsNullOrWhiteSpace(x) -> failwith "Key cannot be empty"
        | x when position.Labels.ContainsKey(x) && position.Labels[x] = value -> position
        | _ when String.IsNullOrWhiteSpace(value) -> failwith "Value cannot be empty"
        | _ ->
            let e = OptionPositionLabelSet(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, key, value)
            apply e position
            
    let setLabelIfValueNotNone key value date position =
        match value with
        | None -> position
        | Some value -> setLabel key value date position
        
    let deleteLabel key date position =
        match key with
        | x when String.IsNullOrWhiteSpace(x) -> failwith "Key cannot be empty"
        | x when position.Labels.ContainsKey(x) |> not -> position
        | _ ->
            let e = OptionPositionLabelDeleted(Guid.NewGuid(), position.PositionId |> OptionPositionId.guid, date, key)
            apply e position
        
            

type OptionPricing =
    {
        UserId: core.fs.Accounts.UserId
        OptionPositionId: OptionPositionId
        UnderlyingTicker: Ticker
        Symbol: OptionTicker
        Expiration: OptionExpiration
        StrikePrice: decimal
        OptionType: OptionType
        Volume: int64
        OpenInterest: int64
        Bid: decimal
        Ask: decimal
        Last: decimal
        Mark: decimal
        Volatility: decimal
        Delta: decimal
        Gamma: decimal
        Theta: decimal
        Vega: decimal
        Rho: decimal
        UnderlyingPrice: decimal option
        Timestamp: DateTimeOffset
    }
