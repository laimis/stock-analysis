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
    Type: StockTransactionType
    NumberOfShares: decimal
    Price: decimal
    Date: DateTimeOffset
}

type StockPositionState =
    {
        PositionId: StockPositionId
        Ticker: Ticker
        NumberOfShares: decimal
        AverageCost: decimal
        Opened: DateTimeOffset
        Closed: DateTimeOffset option
        Transactions: StockPositionTransaction list
        StopPrice: decimal option
        Notes: string list
        Labels: (string * string) list
        
        Version: int
        Events: AggregateEvent list
    }
    member this.Cost = this.NumberOfShares * this.AverageCost
    member this.AggregateId = this.PositionId |> StockPositionId.guid
        
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
    
type StockPositionOpened(id, aggregateId, ``when``, ticker) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
    member this.PositionId = aggregateId |> StockPositionId    
    member this.Ticker = ticker
    
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


module StockPosition =
        
    let isShort stockPosition =
        match stockPosition with
        | x when x.NumberOfShares < 0m -> true
        | _ -> false
    
    let private calculateAverageCost isShort transactions =
        
        let buys,sells =
            transactions
            |> List.partition (fun x ->
                match isShort with
                | true -> x.Type = Sell
                | false -> x.Type = Buy)
            
        // we first build out slots for each buy transaction for each share owned
        let slots = buys |> List.collect (fun x -> List.replicate (int x.NumberOfShares |> abs) x.Price)
            
        // we then go through each sell transaction and remove the shares from the slots
        let slots =
            sells
            |> List.fold (fun (acc:decimal list) x ->
                
                let numberOfShares = x.NumberOfShares
                acc |> List.splitAt (int numberOfShares) |> snd
            ) slots
        
        // we then calculate the average cost of the remaining shares
        slots |> List.average
    
    let createInitialState (event: StockPositionOpened) =
        {
            PositionId = event.PositionId
            Ticker = event.Ticker
            NumberOfShares = 0m
            AverageCost = 0m
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
            let newNumberOfShares = p.NumberOfShares + x.NumberOfShares
            let newTransactions = p.Transactions @ [{ Type = Buy; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            let averageCost = newTransactions |> calculateAverageCost (p |> isShort)
            { p with NumberOfShares = newNumberOfShares; AverageCost = averageCost; Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x] }
        
        | :? StockSold as x ->
            let newNumberOfShares = p.NumberOfShares - x.NumberOfShares
            let newTransactions = p.Transactions @ [{ Type = Sell; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            let averageCost = newTransactions |> calculateAverageCost (p |> isShort)
            { p with NumberOfShares = newNumberOfShares; AverageCost = averageCost; Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionClosed as x ->
            { p with Closed = Some x.When; Version = p.Version + 1; Events = p.Events @ [x]  }
        
        | _ -> failwith ("Unknown event: " + event.GetType().Name)
    
    let createFromEvents (events: AggregateEvent seq) =
        // build state from the first event, casting it to PositionOpened
        let state = events |> Seq.head :?> StockPositionOpened |> createInitialState
        events |> Seq.skip 1 |> Seq.fold (fun acc e -> apply e acc) state
        
    let private applyNotesIfApplicable notes date stockPosition =
        match notes with
            | None -> stockPosition
            | Some notes when String.IsNullOrWhiteSpace(notes) -> stockPosition
            | Some notes ->
                let e = StockPositionNotesAdded(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, notes)
                apply e stockPosition
                
    let private createPosition ticker date notes =
        StockPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker)
        |> createInitialState
        |> applyNotesIfApplicable notes date
    
    let private closePositionIfApplicable date stockPosition =
        match stockPosition.NumberOfShares with
        | 0m -> 
            let e = StockPositionClosed(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date)
            apply e stockPosition
        | _ -> stockPosition
        
    let buy numberOfShares price date notes stockPosition =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        let isShort = stockPosition |> isShort
        
        if isShort && stockPosition.NumberOfShares + numberOfShares > 0m then
            failwith "Cannot buy more than short position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        let e = StockPurchased(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, numberOfShares, price)
        
        apply e stockPosition
        |> applyNotesIfApplicable notes date
        |> closePositionIfApplicable date
        
    let setStop stopPrice date stockPosition =
        match stopPrice with
        | None -> stockPosition
        | Some stopPrice when stopPrice <= 0m -> failwith "Stop price must be greater than zero"
        | Some stopPrice ->    
            let e = StockPositionStopSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, stopPrice)
            apply e stockPosition
        
    let openLong ticker numberOfShares price date stopPrice notes =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
        
        createPosition ticker date notes
        |> buy numberOfShares price date notes
        |> setStop stopPrice date
        
    let openShort ticker numberOfShares price date =
        if numberOfShares >= 0m then
            failwith "Number of shares must be less than zero"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        createPosition ticker date
        
    let sell numberOfShares price date notes stockPosition =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        let isLong = stockPosition |> isShort |> not    
        
        if isLong && stockPosition.NumberOfShares - numberOfShares < 0m then failwith "Cannot sell more than long position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
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