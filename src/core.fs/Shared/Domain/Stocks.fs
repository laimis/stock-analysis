namespace core.fs.Shared.Domain

open System
open core.Shared

type StockPositionId =
    StockPositionId of Guid
      
module StockPositionId =
    let create() = StockPositionId (Guid.NewGuid())
    let guid (StockPositionId id) = id
    let parse (id:string) = id |> Guid.Parse |> StockPositionId
    
type StockTransactionType =
    | Buy
    | Sell
    
type StockPositionTransaction = {
    TransactionId: Guid
    Type: StockTransactionType
    NumberOfShares: decimal
    Price: decimal
    Date: DateTimeOffset
}

type PLTransaction = {
    Date: DateTimeOffset
    NumberOfShares: decimal
    BuyPrice: decimal
    SellPrice: decimal
    Profit: decimal
    GainPct: decimal
}

type StockPositionType =
    | Long
    | Short

type StockPositionState =
    {
        PositionId: StockPositionId
        Ticker: Ticker
        StockPositionType: StockPositionType
        Opened: DateTimeOffset
        Closed: DateTimeOffset option
        Transactions: StockPositionTransaction list
        StopPrice: decimal option
        Notes: string list
        Labels: (string * string) list
        
        Version: int
        Events: AggregateEvent list
    }
    member this.IsShort = this.StockPositionType = Short
    member this.IsClosed = this.Closed.IsSome
    member this.NumberOfShares = this.Transactions |> List.map (fun x -> match x.Type with | Buy -> x.NumberOfShares | Sell -> -x.NumberOfShares) |> List.sum
    member this.AggregateId = this.PositionId |> StockPositionId.guid
        
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
    
type StockPositionOpened(id, aggregateId, ``when``, ticker, positionType) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.PositionId = aggregateId |> StockPositionId    
    member this.Ticker = ticker
    member this.PositionType = positionType
    
type StockPurchased(id, aggregateId, ``when``, numberOfShares, price) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.NumberOfShares = numberOfShares
    member this.Price = price
    
type StockSold(id, aggregateId, ``when``, numberOfShares, price) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.NumberOfShares = numberOfShares
    member this.Price = price

type StockPositionClosed(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)

type StockPositionStopSet(id, aggregateId, ``when``, stopPrice) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.StopPrice = stopPrice

type StockPositionNotesAdded(id, aggregateId, ``when``, notes) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Notes = notes
    
type StockPositionLabelSet(id, aggregateId, ``when``, key, value) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Key = key
    member this.Value = value
    
type StockPositionTransactionDeleted(id, aggregateId, ``when``, transactionId) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.TransactionId = transactionId
    
type StockPositionDeleted(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)

module StockPosition =
        
    let createInitialState (event: StockPositionOpened) =
        {
            PositionId = event.PositionId
            Ticker = event.Ticker
            StockPositionType = event.PositionType 
            Opened = event.When
            Closed = None
            Transactions = []
            StopPrice = None
            Notes = []
            Labels = []
            
            Version = 1
            Events = [event]
        }
        
    let private apply (event: AggregateEvent) p =
        
        match event with
        
        | :? StockPositionOpened as _ ->
            
            p // pass through, this should be done by createInitialState
            
        | :? StockPositionStopSet as x ->
            { p with StopPrice = Some x.StopPrice; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? StockPositionNotesAdded as x ->
            { p with Notes = x.Notes :: p.Notes; Version = p.Version + 1; Events = p.Events @ [x] }
        
        | :? StockPurchased as x ->
            let newTransactions = p.Transactions @ [{ TransactionId = x.Id; Type = Buy; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x] }
        
        | :? StockSold as x ->
            let newTransactions = p.Transactions @ [{ TransactionId = x.Id; Type = Sell; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionClosed as x ->
            { p with Closed = Some x.When; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionLabelSet as x ->
            { p with Labels = (x.Key, x.Value) :: p.Labels; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionTransactionDeleted as x ->
            // truncate transactions by removing the last one
            let newTransactions = p.Transactions |> List.rev |> List.tail |> List.rev
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | _ -> failwith ("Unknown event: " + event.GetType().Name)
    
    let failIfInvalidDate date =
        if date < DateTimeOffset.UnixEpoch then
            failwith ("Date after " + DateTimeOffset.UnixEpoch.ToString() + " is required")
            
        if date.Subtract(DateTimeOffset.UtcNow).TotalHours >= 12 then
            failwith "Date cannot be in the future"
        
    let createFromEvents (events: AggregateEvent seq) =
        // build state from the first event, casting it to PositionOpened
        let state = events |> Seq.head :?> StockPositionOpened |> createInitialState
        events |> Seq.skip 1 |> Seq.fold (fun acc e -> apply e acc) state
        
    let private applyNotesIfApplicable notes date stockPosition =
        match notes with
            | None -> stockPosition
            | Some notes when String.IsNullOrWhiteSpace(notes) -> stockPosition
            | Some notes when stockPosition.Notes |> List.contains notes -> stockPosition
            | Some notes ->
                let e = StockPositionNotesAdded(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, notes)
                apply e stockPosition
        
    let private closePositionIfApplicable date (stockPosition:StockPositionState) =
        match stockPosition.NumberOfShares with
        | 0m -> 
            let e = StockPositionClosed(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date)
            apply e stockPosition
        | _ -> stockPosition
        
    let buy numberOfShares price date notes (stockPosition:StockPositionState) =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
            
        date |> failIfInvalidDate
        
        if stockPosition.IsShort && stockPosition.NumberOfShares + numberOfShares > 0m then
            failwith "Cannot buy more than short position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        let e = StockPurchased(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, numberOfShares, price)
        
        apply e stockPosition
        |> applyNotesIfApplicable notes date
        |> closePositionIfApplicable date
        
    let delete stockPosition =
        let e = StockPositionDeleted(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow)
        apply e stockPosition
        
    let setStop stopPrice date (stockPosition:StockPositionState) =
        if stockPosition.IsClosed then failwith "Cannot set stop price on closed position"
        
        match stopPrice with
        | None -> stockPosition
        | Some stopPrice when stopPrice <= 0m -> failwith "Stop price must be greater than zero"
        | Some stopPrice when stockPosition.StopPrice.IsSome && stopPrice = stockPosition.StopPrice.Value -> stockPosition
        | Some stopPrice ->    
            let e = StockPositionStopSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, stopPrice)
            apply e stockPosition
        
    let openLong ticker date =
        
        date |> failIfInvalidDate
        
        StockPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker, Long)
        |> createInitialState
        
    let sell numberOfShares price date notes (stockPosition:StockPositionState) =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        if stockPosition.IsShort |> not && stockPosition.NumberOfShares - numberOfShares < 0m then failwith "Cannot sell more than long position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        date |> failIfInvalidDate
            
        let e = StockSold(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, numberOfShares, price)
        
        apply e stockPosition
        |> applyNotesIfApplicable notes date
        |> closePositionIfApplicable date
        
    let addNotes = applyNotesIfApplicable
        
    let setLabel key value date stockPosition =
        if String.IsNullOrWhiteSpace(key) then
            failwith "Key cannot be empty"
            
        let e = StockPositionLabelSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, key, value)
        apply e stockPosition
        
    let deleteTransaction transactionId stockPosition =
        
        let lastTransaction = stockPosition.Transactions |> List.last
        
        if lastTransaction.TransactionId <> transactionId then
            failwith "Can only delete last transaction"
            
        let e = StockPositionTransactionDeleted(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, DateTimeOffset.UtcNow, transactionId)
        apply e stockPosition
        
        
type StockPositionWithCalculations(stockPosition:StockPositionState) =
    
    let sells,buys =
        stockPosition.Transactions
        |> List.partition (fun x ->
            match stockPosition.IsShort with
            | true -> x.Type = Buy
            | false -> x.Type = Sell)
        
    let buySlots = buys |> List.collect (fun x -> List.replicate (int x.NumberOfShares |> abs) x.Price)
    let sellSlots = sells |> List.collect (fun x -> List.replicate (int x.NumberOfShares |> abs) x.Price)
            
    member this.IsShort = stockPosition.IsShort
    member this.Transactions = stockPosition.Transactions
    member this.PositionId = stockPosition.PositionId
    member this.Ticker = stockPosition.Ticker
    member this.NumberOfShares = stockPosition.NumberOfShares
    member this.Opened = stockPosition.Opened
    member this.Closed = stockPosition.Closed
    member this.StopPrice = stockPosition.StopPrice
    member this.Notes = stockPosition.Notes
    member this.AverageCostPerShare =
        
        let sharesSoldTotal = sells |> List.sumBy (fun x -> x.NumberOfShares |> abs) |> int
        let remainingShares = buySlots |> List.skip sharesSoldTotal
        
        // we then calculate the average cost of the remaining shares
        match remainingShares with
        | [] -> 0m
        | _ -> remainingShares |> List.average
        
    member this.Cost = stockPosition.NumberOfShares * this.AverageCostPerShare
    member this.DaysHeld =
        let referenceDay =
            match stockPosition.Closed with
            | None -> DateTimeOffset.UtcNow
            | Some x -> x   
        referenceDay.Subtract(stockPosition.Opened).TotalDays |> int
    member this.DaysSinceLastTransaction =
        this.Transactions |> List.last |> fun x -> DateTimeOffset.UtcNow.Subtract(x.Date).TotalDays |> int
        
    member this.Profit =
        // profit is generating whenever we sell shares of a long position or buy shares of a short position
        sellSlots
        |> List.indexed
        |> List.fold (fun (acc:decimal) (index, sellPrice) ->
            
            let buyPrice = buySlots[index]
            let profit = sellPrice - buyPrice
            acc + profit
        ) 0m
        
    member this.AverageBuyCostPerShare =
        match buySlots with
            | [] -> 0m
            | _ -> buySlots |> List.average
            
    member this.AverageSaleCostPerShare =
        match sellSlots with
            | [] -> 0m
            | _ -> sellSlots |> List.average
        
    member this.GainPct =
        match this.AverageSaleCostPerShare with
        | 0m -> 0m // we haven't sold any, no gain pct
        | _ -> (this.AverageSaleCostPerShare - this.AverageBuyCostPerShare) / this.AverageBuyCostPerShare
        
    member this.PLTransactions =
        // we fold over sells, and for each sell create a PLTransaction
        sells
        |> List.fold (fun (acc:PLTransaction list) (sell:StockPositionTransaction) ->
            
            let offset =
                match acc with
                [] -> 0m
                | _ -> acc |> List.sumBy _.NumberOfShares
            
            let slotsOfInterest =
                buySlots
                |> List.skip (int offset)
                |> List.take (sell.NumberOfShares |> abs |> int)
                
            let buyPrice = slotsOfInterest |> List.average
            let profit = sell.NumberOfShares * (sell.Price - buyPrice)
            let gainPct = (sell.Price - buyPrice) / buyPrice
            
            // we then create a PLTransaction
            let pl = {
                Date = sell.Date
                Profit = profit
                BuyPrice = buyPrice
                GainPct = gainPct
                SellPrice = sell.Price
                NumberOfShares = sell.NumberOfShares 
            }
            
            // and add it to the accumulator
            acc @ [pl]
        ) []