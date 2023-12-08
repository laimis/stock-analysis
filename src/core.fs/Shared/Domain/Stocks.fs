namespace core.fs.Shared.Domain

open System
open core.Shared

type StockPositionId = StockPositionId of Guid
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

type StockPositionState = {
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
    with interface IAggregate with
             member this.Events = this.Events
             member this.Version = this.Version
             

type StockPositionOpened(positionId, ticker, numberOfShares, price, date, stopPrice:decimal option) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
    member this.Ticker = ticker
    member this.NumberOfShares = numberOfShares
    member this.Price = price
    member this.StopPrice = stopPrice
    
type StockPurchased(positionId, numberOfShares, price, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
    member this.NumberOfShares = numberOfShares
    member this.Price = price
    
type StockSold(positionId, numberOfShares, price, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
    member this.NumberOfShares = numberOfShares
    member this.Price = price

type StockPositionClosed(positionId, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)

type StockPositionStopSet(positionId, stopPrice, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
    member this.StopPrice = stopPrice

type StockPositionNotesAdded(positionId, notes, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
    member this.Notes = notes
    
type StockPositionLabelSet(positionId, key, value, date) =
    inherit AggregateEvent(Guid.NewGuid(), positionId |> StockPositionId.guid, date)
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
    
    let private apply (event: AggregateEvent) (p: StockPositionState option) =
        match event with
        
        | :? StockPositionOpened as x ->
            
            let p = {
                PositionId = x.AggregateId |> StockPositionId
                Ticker = x.Ticker
                NumberOfShares = x.NumberOfShares
                AverageCost = x.Price
                Opened = x.When
                Closed = None
                Transactions = []
                StopPrice = x.StopPrice
                Notes = []
                Labels = []
                
                Version = 1
                Events = [x]
            }
            
            Some p
            
        | :? StockPositionStopSet as x ->
            let p = p.Value
            Some { p with StopPrice = Some x.StopPrice; Version = p.Version + 1; Events = event :: p.Events }
            
        | :? StockPositionNotesAdded as x ->
            let p = p.Value
            Some { p with Notes = x.Notes :: p.Notes; Version = p.Version + 1; Events = event :: p.Events }
        
        | :? StockPurchased as x ->
            let p = p.Value
            let newNumberOfShares = p.NumberOfShares + x.NumberOfShares
            let newTransactions = { Type = Buy; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When } :: p.Transactions
            let averageCost = newTransactions |> calculateAverageCost (p |> isShort)
            Some { p with NumberOfShares = newNumberOfShares; AverageCost = averageCost; Transactions = newTransactions; Version = p.Version + 1; Events = event :: p.Events }
        
        | :? StockSold as x ->
            let p = p.Value
            let newNumberOfShares = p.NumberOfShares - x.NumberOfShares
            let newTransactions = { Type = Sell; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When } :: p.Transactions
            let averageCost = newTransactions |> calculateAverageCost (p |> isShort)
            Some { p with NumberOfShares = newNumberOfShares; AverageCost = averageCost; Transactions = newTransactions; Version = p.Version + 1; Events = event :: p.Events }
            
        | :? StockPositionClosed as x ->
            let p = p.Value
            Some { p with Closed = Some x.When; Version = p.Version + 1; Events = event :: p.Events }
        
        | _ -> failwith "Unknown event"
    
    let createFromEvents (events: AggregateEvent seq) =
        events |> Seq.fold (fun acc e -> apply e acc) None
        
    let private applyNotesIfApplicable notes date (positionOption:StockPositionState option) =
        match notes with
            | None -> positionOption
            | Some notes when String.IsNullOrWhiteSpace(notes) -> positionOption
            | Some notes ->
                let e = StockPositionNotesAdded(positionOption.Value.PositionId, notes, date)
                apply e positionOption
                
    let createPosition ticker numberOfShares price date stopPrice notes =
        let id = StockPositionId.create()
        let e = StockPositionOpened(id, ticker, numberOfShares, price, date, stopPrice)
        apply e None |> applyNotesIfApplicable notes date
        
    let openLong ticker numberOfShares price date =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        createPosition ticker numberOfShares price date
        
    let openShort ticker numberOfShares price date =
        if numberOfShares >= 0m then
            failwith "Number of shares must be less than zero"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        createPosition ticker numberOfShares price date
        
    let buy numberOfShares price date notes stockPosition =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        let isShort = stockPosition |> isShort
        
        if isShort && stockPosition.NumberOfShares + numberOfShares > 0m then
            failwith "Cannot buy more than short position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        let e = StockPurchased(stockPosition.PositionId, numberOfShares, price, date)
        
        let stockPositionOption = apply e (Some stockPosition) |> applyNotesIfApplicable notes date
        
        match isShort with
        | true ->
            // check if we need to mark it as closed
            match stockPositionOption with
            | Some x when x.NumberOfShares = 0m ->
                let e = StockPositionClosed(stockPosition.PositionId, date)
                apply e stockPositionOption
            | _ -> stockPositionOption
                
        | false -> stockPositionOption
        
        
    let sell numberOfShares price date notes stockPosition=
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        let isLong = stockPosition |> isShort |> not    
        
        if isLong && stockPosition.NumberOfShares - numberOfShares < 0m then failwith "Cannot sell more than long position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        let e = StockSold(stockPosition.PositionId, numberOfShares, price, date)
        
        let stockPositionOption = apply e (Some stockPosition) |> applyNotesIfApplicable notes date
        
        match isLong with
        | true ->
            // check if we need to mark it as closed
            match stockPositionOption with
            | Some x when x.NumberOfShares = 0m ->
                let e = StockPositionClosed(stockPosition.PositionId, date)
                apply e stockPositionOption
            | _ -> stockPositionOption
        | false -> stockPositionOption
        
    let setStop stopPrice date stockPosition =
        if stopPrice <= 0.0m then
            failwith "Stop price must be greater than zero"
            
        let e = StockPositionStopSet(stockPosition.PositionId, stopPrice, date)
        apply e (Some stockPosition)
        
    let addNotes notes date stockPosition =
        match notes with
        | None -> Some stockPosition
        | Some notes when String.IsNullOrWhiteSpace(notes) -> Some stockPosition
        | Some notes ->
            let e = StockPositionNotesAdded(stockPosition.PositionId, notes, date)
            apply e (Some stockPosition)
            
    let setLabel key value date stockPosition =
        if String.IsNullOrWhiteSpace(key) then
            failwith "Key cannot be empty"
            
        let e = StockPositionLabelSet(stockPosition.PositionId, key, value, date)
        apply e (Some stockPosition)