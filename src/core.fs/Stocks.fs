namespace core.fs.Stocks

open System
open System.Collections.Generic
open core.Shared

type StockPositionId = StockPositionId of Guid

module StockPositionId =
    let create() = StockPositionId (Guid.NewGuid())
    let guid (StockPositionId id) = id
    let parse (id:string) = id |> Guid.Parse |> StockPositionId
    
type StockTransactionType =
    | Buy
    | Sell
    
type StockPositionShareTransaction = {
    Ticker:Ticker
    TransactionId: Guid
    Type: StockTransactionType
    NumberOfShares: decimal
    Price: decimal
    Date: DateTimeOffset
}

type StockPositionStopTransaction = {
    TransactionId: Guid
    StopPrice: decimal option
    Date: DateTimeOffset
}

type StockPositionRiskTransaction = {
    TransactionId: Guid
    RiskAmount: decimal
    Date: DateTimeOffset
}

type StockPositionDividendTransaction = {
    Ticker: Ticker
    ActivityId: string
    Description: string
    NetAmount: decimal
    Date: DateTimeOffset
}

type StockPositionFeeTransaction = {
    Ticker: Ticker
    ActivityId: string
    Description: string
    NetAmount: decimal
    Date: DateTimeOffset
}

type PLTransaction = {
    Ticker:Ticker
    Date: DateTimeOffset
    NumberOfShares: decimal
    BuyPrice: decimal
    SellPrice: decimal
    Profit: decimal
    GainPct: decimal
    Type: StockTransactionType
}

type StockPositionTransaction =
    | Share of StockPositionShareTransaction
    | Stop of StockPositionStopTransaction
    | Risk of StockPositionRiskTransaction
    | Dividend of StockPositionDividendTransaction
    | Fee of StockPositionFeeTransaction

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
        RiskAmount: decimal option
        Notes: core.fs.Note list
        Labels: Dictionary<string, string>
        Grade: TradeGrade option
        GradeNote : string option
        
        Version: int
        Events: AggregateEvent list
    }
    // member this.IsShort = this.StockPositionType = Short
    member this.IsClosed = this.Closed.IsSome
    member this.IsOpen = this.Closed.IsNone
    member this.HasStopPrice = this.StopPrice.IsSome
    member this.ShareTransactions  =
        this.Transactions
        |> List.map (fun x -> match x with | Share s -> Some s | _ -> None)
        |> List.choose id
        
    member this.NumberOfShares =
        this.ShareTransactions
        |> List.map (fun x -> match x.Type with | Buy -> x.NumberOfShares | Sell -> -x.NumberOfShares) |> List.sum
        
    member this.HasLabel key value =
        this.Labels.ContainsKey(key) && this.Labels[key] = value
        
    member this.TryGetLabelValue key =
        match this.Labels.ContainsKey(key) with
        | true -> Some this.Labels[key]
        | false -> None
    
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

type StockPositionStopSet(id, aggregateId, ``when``, stopPrice:decimal) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.StopPrice = stopPrice
    
type StockPositionStopDeleted(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    
type StockPositionRiskAmountSet(id, aggregateId, ``when``, riskAmount) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.RiskAmount = riskAmount

type StockPositionNotesAdded(id, aggregateId, ``when``, notes:string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Notes = notes
    
type StockPositionLabelSet(id, aggregateId, ``when``, key, value) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Key = key
    member this.Value = value
    
type StockPositionLabelDeleted(id, aggregateId, ``when``, key) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Key = key
    
type StockPositionTransactionDeleted(id, aggregateId, ``when``, transactionId) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.TransactionId = transactionId
    
type StockPositionDeleted(id, aggregateId, ``when``) =
    inherit AggregateEvent(id, aggregateId, ``when``)

type StockPositionGradeAssigned(id, aggregateId, ``when``, grade, gradeNote) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.Grade = grade
    member this.GradeNote = gradeNote
    
type StockPositionDividend(id, aggregateId, ``when``, activityId, description, netAmount) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.ActivityId = activityId
    member this.Description = description
    member this.NetAmount = netAmount
    
type StockPositionDividendReinvested(id, aggregateId, ``when``, activityId, numberOfShares, price) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.ActivityId = activityId
    member this.NumberOfShares = numberOfShares
    member this.Price = price
    
type StockPositionFee(id, aggregateId, ``when``, activityId, description, netAmount) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member this.ActivityId = activityId
    member this.Description = description
    member this.NetAmount = netAmount
    
module StockPosition =
        
    let createInitialState (event: StockPositionOpened) =
        {
            PositionId = event.PositionId
            Ticker = event.Ticker |> Ticker
            StockPositionType = event.PositionType 
            Opened = event.When
            Closed = None
            Transactions = []
            StopPrice = None
            RiskAmount = None 
            Notes = []
            Labels = Dictionary<string, string>()
            Grade = None
            GradeNote = None 
            Version = 1
            Events = [event]
        }
        
    let private apply (event: AggregateEvent) p =
        
        match event with
        
        | :? StockPositionOpened as _ ->
            
            p // pass through, this should be done by createInitialState
            
        | :? StockPositionStopSet as x ->
            let newTransactions = p.Transactions @ [Stop { TransactionId = x.Id; StopPrice = Some x.StopPrice; Date = x.When }]
            { p with Transactions = newTransactions; StopPrice = Some x.StopPrice; Version = p.Version + 1; Events = p.Events @ [x] }
            
        | :? StockPositionNotesAdded as x ->
            { p with Notes = p.Notes @ [{created = x.When; content = x.Notes; id = x.Id }]; Version = p.Version + 1; Events = p.Events @ [x] }
        
        | :? StockPurchased as x ->
            let newTransactions = p.Transactions @ [Share { Ticker = p.Ticker; TransactionId = x.Id; Type = Buy; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x] }
        
        | :? StockSold as x ->
            let newTransactions = p.Transactions @ [Share { Ticker = p.Ticker; TransactionId = x.Id; Type = Sell; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionClosed as x ->
            { p with Closed = Some x.When; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionLabelSet as x ->
            p.Labels[x.Key] <- x.Value
            { p with Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionTransactionDeleted as x ->
            // truncate transactions by removing the last one
            let newTransactions = p.Transactions |> List.rev |> List.tail |> List.rev
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionLabelDeleted as x ->
            p.Labels.Remove(x.Key) |> ignore
            { p with Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionRiskAmountSet as x ->
            let newTransactions = p.Transactions @ [ Risk { TransactionId = x.Id; RiskAmount = x.RiskAmount; Date = x.When } ]
            { p with Transactions = newTransactions; RiskAmount = Some x.RiskAmount; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionGradeAssigned as x ->
            let gradeValue = x.Grade |> TradeGrade |> Some
            { p with Grade = gradeValue; GradeNote = x.GradeNote |> Some; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionStopDeleted as x ->
            let newTransactions = p.Transactions @ [Stop {TransactionId = x.Id; StopPrice = None; Date = x.When }]
            { p with StopPrice = None; Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionDeleted as _ ->
            p // no op right now, not sure if I want to keep those around and marked as deleted but right now I am deleting them
        
        | :? StockPositionDividend as x ->
            let newTransactions = p.Transactions @ [Dividend { ActivityId = x.ActivityId; Description = x.Description; NetAmount = x.NetAmount; Date = x.When; Ticker = p.Ticker }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionDividendReinvested as x ->
            let newTransactions = p.Transactions @ [Share { Ticker = p.Ticker; TransactionId = x.Id; Type = Buy; NumberOfShares = x.NumberOfShares; Price = x.Price; Date = x.When }]
            { p with Transactions = newTransactions; Version = p.Version + 1; Events = p.Events @ [x]  }
            
        | :? StockPositionFee as x ->
            let newTransactions = p.Transactions @ [Fee { ActivityId = x.ActivityId; Description = x.Description; NetAmount = x.NetAmount; Date = x.When; Ticker = p.Ticker }]
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
            | Some notes when stockPosition.Notes |> List.exists (fun n -> n.content = notes) -> stockPosition
            | Some notes ->
                let e = StockPositionNotesAdded(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, notes)
                apply e stockPosition
        
    let private closePositionIfApplicable date (stockPosition:StockPositionState) =
        match stockPosition.NumberOfShares with
        | 0m -> 
            let e = StockPositionClosed(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date)
            apply e stockPosition
        | _ -> stockPosition
        
    let buy numberOfShares price date (stockPosition:StockPositionState) =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
            
        date |> failIfInvalidDate
        
        if stockPosition.StockPositionType = Short && stockPosition.NumberOfShares + numberOfShares > 0m then
            failwith "Cannot buy more than short position"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        let e = StockPurchased(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, numberOfShares, price)
        
        apply e stockPosition
        |> closePositionIfApplicable date
        
    let sell numberOfShares price date (stockPosition:StockPositionState) =
        if numberOfShares <= 0m then
            failwith "Number of shares must be greater than zero"
        
        if stockPosition.StockPositionType = Short |> not && stockPosition.NumberOfShares - numberOfShares < 0m then failwith $"Cannot sell more than long position: {stockPosition.NumberOfShares} - {numberOfShares} < 0"
        
        if price <= 0.0m then
            failwith "Price must be greater than zero"
            
        date |> failIfInvalidDate
            
        let e = StockSold(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, numberOfShares, price)
        
        apply e stockPosition
        |> closePositionIfApplicable date
        
    let assignGrade grade (gradeNote:string option) date (stockPosition:StockPositionState) =
        match stockPosition with
        | x when x.Closed.IsNone -> failwith "Cannot assign grade to open position"
        | _ when gradeNote.IsNone || String.IsNullOrWhiteSpace(gradeNote.Value) -> failwith "Grade notes cannot be empty"
        | x when x.Grade.IsSome && x.Grade.Value = grade && x.GradeNote.Value = gradeNote.Value -> stockPosition
        | _ ->
            let e = StockPositionGradeAssigned(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, grade.Value, gradeNote.Value)
            apply e stockPosition
            |> applyNotesIfApplicable gradeNote date
        
    let delete stockPosition =
        let e = StockPositionDeleted(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, DateTimeOffset.UtcNow)
        apply e stockPosition
    
    let setRiskAmount riskAmount date stockPosition =
        let e = StockPositionRiskAmountSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, riskAmount)
        apply e stockPosition
            
    let setStop stopPrice date (stockPosition:StockPositionState) =
        
        let withStop =
            match stopPrice with
            | None -> stockPosition
            | Some stopPrice when stopPrice <= 0m -> failwith "Stop price must be greater than zero"
            | Some stopPrice when stockPosition.StopPrice.IsSome && stopPrice = stockPosition.StopPrice.Value -> stockPosition
            | Some stopPrice ->    
                let e = StockPositionStopSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, stopPrice)
                apply e stockPosition
        
        // if stop price is set, we should set risked amount if it's not set
        match withStop.StopPrice with
        | None ->
            withStop
        | Some _ when withStop.RiskAmount.IsSome ->
            withStop
        | Some stopPrice ->
            let riskAmount = 
                withStop.ShareTransactions
                |> List.takeWhile (fun x -> match withStop.StockPositionType with | Short -> x.Type = Sell | Long -> x.Type = Buy)
                |> List.sumBy (fun x -> x.NumberOfShares * (x.Price - stopPrice)) |> abs
                
            withStop |> setRiskAmount riskAmount date
    
    let deleteStop date (stockPosition:StockPositionState) =
        if stockPosition.IsClosed then failwith "Cannot delete stop price on closed position"
        
        match stockPosition.StopPrice with
        | None -> stockPosition
        | Some _ ->
            let e = StockPositionStopDeleted(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date)
            apply e stockPosition
            
    let openLong (ticker:Ticker) date =
        
        date |> failIfInvalidDate
        
        StockPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker.Value, Long)
        |> createInitialState
        
    let openShort (ticker:Ticker) date =
        
        date |> failIfInvalidDate
        
        StockPositionOpened(Guid.NewGuid(), Guid.NewGuid(), date, ticker.Value, Short)
        |> createInitialState
        
    let ``open`` (ticker:Ticker) (numberOfShares:decimal) price date =
        match numberOfShares > 0m with
        | true -> openLong ticker date |> buy numberOfShares price date
        | false -> openShort ticker date |> sell (numberOfShares |> abs) price date
        
    let close price date (stockPosition:StockPositionState) =
        match stockPosition.IsClosed with
        | true -> failwith "Position is already closed"
        | false ->
            match stockPosition.StockPositionType with
            | Short -> buy (stockPosition.NumberOfShares |> abs) price date stockPosition
            | Long -> sell stockPosition.NumberOfShares price date stockPosition
        
    let addNotes = applyNotesIfApplicable
        
    let setLabel key value date stockPosition =
        
        match key with
        | x when String.IsNullOrWhiteSpace(x) -> failwith "Key cannot be empty"
        | x when stockPosition.Labels.ContainsKey(x) && stockPosition.Labels[x] = value -> stockPosition
        | _ when String.IsNullOrWhiteSpace(value) -> failwith "Value cannot be empty"
        | _ ->
            let e = StockPositionLabelSet(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, key, value)
            apply e stockPosition
            
    let setLabelIfValueNotNone key value date stockPosition =
        match value with
        | None -> stockPosition
        | Some value -> setLabel key value date stockPosition
            
    let deleteLabel key date stockPosition =
        
        match key with
        | x when String.IsNullOrWhiteSpace(x) -> failwith "Key cannot be empty"
        | x when stockPosition.Labels.ContainsKey(x) |> not -> stockPosition
        | _ ->
            let e = StockPositionLabelDeleted(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, key)
            apply e stockPosition
        
    let deleteTransaction transactionId date stockPosition =
        
        let lastTransaction =
            stockPosition.Transactions
            |> List.map (fun x -> match x with | Share s -> Some s | _ -> None)
            |> List.choose id
            |> List.last
        
        if lastTransaction.TransactionId <> transactionId then
            failwith "Can only delete last transaction"
            
        let e = StockPositionTransactionDeleted(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, transactionId)
        apply e stockPosition
        
    let processDividend activityId date description netAmount stockPosition =
        
        // check if the dividend is already processed
        let alreadyProcessed =
            stockPosition.Transactions
            |> List.map (fun x -> match x with | Dividend s -> Some s | _ -> None)
            |> List.choose id
            |> List.exists (fun x -> x.ActivityId = activityId)
            
        match alreadyProcessed with
        | true -> stockPosition
        | false ->
            let e = StockPositionDividend(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, activityId, description, netAmount)
            apply e stockPosition
    
    let reinvestDividend activityId price date stockPosition =
        if price <= 0m then
            failwith "Price must be greater than zero"
            
        // find the dividend transaction
        let activity = stockPosition.Transactions |> List.tryFind (fun x -> match x with | Dividend s -> s.ActivityId = activityId | _ -> false)
        match activity with
        | Some (Dividend dividend) ->
            let numberOfShares = dividend.NetAmount / price
            let e = StockPositionDividendReinvested(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, activityId, numberOfShares, price)
            apply e stockPosition
        | Some _ -> failwith "Found transaction but it's not a dividend"
        | None -> failwith "Dividend not found"
            
    let processFee activityId date description netAmount stockPosition =
        
        // check if the fee is already processed
        let alreadyProcessed =
            stockPosition.Transactions
            |> List.map (fun x -> match x with | Fee s -> Some s | _ -> None)
            |> List.choose id
            |> List.exists (fun x -> x.ActivityId = activityId)
            
        match alreadyProcessed with
        | true -> stockPosition
        | false ->
            let e = StockPositionFee(Guid.NewGuid(), stockPosition.PositionId |> StockPositionId.guid, date, activityId, description, netAmount)
            apply e stockPosition
        
type StockPositionWithCalculations(stockPosition:StockPositionState) =
    
    let sells,buys =
        stockPosition.Transactions
        |> List.map (fun x -> match x with | Share s -> Some s | _ -> None)
        |> List.choose id
        |> List.partition (fun x -> x.Type = Sell)
        
    let riskAmounts =
        stockPosition.Transactions
        |> List.map (fun x -> match x with | Risk s -> Some s | _ -> None)
        |> List.choose id
        
    let dividends =
        stockPosition.Transactions
        |> List.map (fun x -> match x with | Dividend s -> Some s | _ -> None)
        |> List.choose id
    
    let dividendTotal = dividends |> List.sumBy _.NetAmount
    
    let fees =
        stockPosition.Transactions
        |> List.map (fun x -> match x with | Fee s -> Some s | _ -> None)
        |> List.choose id
        
    let feeTotal = fees |> List.sumBy _.NetAmount
        
    let buySlots = buys |> List.collect (fun x -> List.replicate (int x.NumberOfShares |> abs) x.Price)
    let sellSlots = sells |> List.collect (fun x -> List.replicate (int x.NumberOfShares |> abs) x.Price)
    
    let liquidationSource, liquidationSlots, acquisitionSource, acquisitionSlots = 
        match stockPosition.StockPositionType with
        | Short -> buys, buySlots, sells, sellSlots
        | Long -> sells, sellSlots, buys, buySlots
    
    let completedPositionTransactions =
        stockPosition.ShareTransactions
        |> List.takeWhile (fun x -> match stockPosition.StockPositionType with | Short -> x.Type = Sell | Long -> x.Type = Buy)
        
    let transactions =
        stockPosition.ShareTransactions
        |> List.map (fun s ->
            let ``type`` = (match s.Type with | Buy -> "Buy" | Sell -> "Sell") |> _.ToLower()
            let description = $"{``type``} {s.NumberOfShares} @ {s.Price}"
            let date = s.Date.ToString("yyyy-MM-dd")
            {|transactionId = s.TransactionId; date = date; price = s.Price; ``type`` = ``type``; description = description; numberOfShares = s.NumberOfShares |}
        )
        
    let plTransactions =
        // we fold over sells, and for each sell create a PLTransaction
        liquidationSource
        |> List.fold (fun (acc:PLTransaction list) (liquidation:StockPositionShareTransaction) ->
            
            let offset =
                match acc with
                [] -> 0m
                | _ -> acc |> List.sumBy _.NumberOfShares
            
            let slotsOfInterest =
                acquisitionSlots
                |> List.skip (int offset)
                |> List.take (liquidation.NumberOfShares |> abs |> int)
                
            let averageAcquisitionPrice = slotsOfInterest |> List.average
            let profitPerShare, gainPct =
                match stockPosition.StockPositionType with
                | Long ->
                    (
                        liquidation.Price - averageAcquisitionPrice,
                        (liquidation.Price - averageAcquisitionPrice) / averageAcquisitionPrice
                    )
                | Short ->
                    (
                        averageAcquisitionPrice - liquidation.Price,
                        (averageAcquisitionPrice - liquidation.Price) / liquidation.Price
                    )
            let profit = liquidation.NumberOfShares * profitPerShare
            
            // we then create a PLTransaction
            let pl = {
                Ticker = stockPosition.Ticker
                Date = liquidation.Date
                Profit = profit
                BuyPrice = averageAcquisitionPrice
                GainPct = gainPct
                SellPrice = liquidation.Price
                NumberOfShares = liquidation.NumberOfShares
                Type = liquidation.Type 
            }
            
            // and add it to the accumulator
            acc @ [pl]
        ) []
        
    let profit = 
        // profit is generating whenever we sell shares of a long position or buy shares of a short position
        liquidationSlots
        |> List.indexed
        |> List.fold (fun (acc:decimal) (index, liquidationPrice) ->
            
            let acquisitionPrice = acquisitionSlots[index]
            let profit =
                match stockPosition.StockPositionType with
                | Long -> liquidationPrice - acquisitionPrice
                | Short -> acquisitionPrice - liquidationPrice
            acc + profit
        ) 0m
    
    let averageBuyCostPerShare =
        match acquisitionSlots with
            | [] -> 0m
            | _ -> acquisitionSlots |> List.average
            
            
    let averageSaleCostPerShare =
        match liquidationSlots with
            | [] -> 0m
            | _ -> liquidationSlots |> List.average
            
    let averageCostPerShare =
        let liquidatedTotal = liquidationSource |> List.sumBy _.NumberOfShares |> int
        let remainingShares = acquisitionSlots |> List.skip liquidatedTotal
        match remainingShares with
        | [] -> averageBuyCostPerShare
        | _ -> remainingShares |> List.average
        
    let events =
        stockPosition.Transactions
        |> List.map (fun t ->
            match t with
            | Share s ->
               let ``type`` = match s.Type with | Buy -> "buy" | Sell -> "sell" |> _.ToLower()
               let description = $"{``type``} {s.NumberOfShares} @ {s.Price}"
               {|id = s.TransactionId.ToString(); date = s.Date; value = s.Price; ``type`` = ``type``; description = description; quantity = s.NumberOfShares |}
            | Risk r ->
                let ``type`` = "risk"
                let description = $"risk amount set to {r.RiskAmount}"
                {|id = r.TransactionId.ToString(); date = r.Date; value = r.RiskAmount; ``type`` = ``type``; description = description; quantity = 0m |}
            | Stop s ->
               let ``type`` = "stop"
               let description =
                   match s.StopPrice with
                   | Some stopPrice -> $"stop price set to {stopPrice}"
                   | None -> "stop deleted"
               {|id = s.TransactionId.ToString(); date = s.Date; value = (s.StopPrice |> Option.defaultValue 0m); ``type`` = ``type``; description = description; quantity = 0m |}
            | Dividend d ->
                let ``type`` = "dividend"
                let description = $"dividend of {d.NetAmount}"
                {|id = d.ActivityId; date = d.Date; value = d.NetAmount; ``type`` = ``type``; description = description; quantity = 0m |}
            | Fee f ->
                let ``type`` = "fee"
                let description = $"{f.Description} in the amount of {f.NetAmount}"
                {|id = f.ActivityId; date = f.Date; value = f.NetAmount; ``type`` = ``type``; description = description; quantity = 0m |}
        )
            
    member this.IsShort = stockPosition.StockPositionType = Short
    member this.StockPositionType = stockPosition.StockPositionType.ToString()
    member this.PositionId = stockPosition.PositionId
    member this.Ticker = stockPosition.Ticker
    member this.NumberOfShares = stockPosition.NumberOfShares
    member this.Opened = stockPosition.Opened
    member this.Closed = stockPosition.Closed
    member this.IsClosed = stockPosition.IsClosed
    member this.IsOpen = stockPosition.IsOpen
    member this.StopPrice = stockPosition.StopPrice
    member this.Notes = stockPosition.Notes
    member this.Grade = stockPosition.Grade
    member this.GradeNote = stockPosition.GradeNote
    
    member this.AverageCostPerShare = averageCostPerShare
        
    member this.Cost =
        match this.IsOpen with
        | true -> stockPosition.NumberOfShares * this.AverageCostPerShare
        | false -> this.CompletedPositionShares * this.CompletedPositionCostPerShare
    
    member this.DaysHeld =
        let referenceDay =
            match stockPosition.Closed with
            | None -> DateTimeOffset.UtcNow
            | Some x -> x   
        referenceDay.Subtract(stockPosition.Opened).TotalDays |> int
        
    member this.DaysSinceLastTransaction =
        let date =
            stockPosition.ShareTransactions
            |> List.last
            |> _.Date
                
        DateTimeOffset.UtcNow.Subtract(date).TotalDays |> int
        
    member this.Profit = profit + dividendTotal + feeTotal
    
    member this.ProfitWithoutDividendsAndFees = profit
        
    member this.AverageBuyCostPerShare = averageBuyCostPerShare
            
    member this.AverageSaleCostPerShare = averageSaleCostPerShare
        
    member this.GainPct =
        match this.IsShort with
        | true ->
            match this.AverageSaleCostPerShare = 0m with
            | true -> 0m
            | false -> (this.AverageBuyCostPerShare - this.AverageSaleCostPerShare) / this.AverageSaleCostPerShare
        | false ->
            match this.AverageBuyCostPerShare = 0m with
            | true -> 0m
            | false -> (this.AverageSaleCostPerShare - this.AverageBuyCostPerShare) / this.AverageBuyCostPerShare
        
    member this.Transactions = transactions
        
    member this.PLTransactions = plTransactions
    
    member this.Dividends = dividends
    
    member this.Fees = fees
        
    member this.RiskedAmount = stockPosition.RiskAmount
    
    member this.InitialRiskedAmount = riskAmounts |> List.tryHead |> Option.map _.RiskAmount
    
    member this.PositionOpenPrice =
        match acquisitionSlots with
        | [] -> 0m
        | _ -> acquisitionSlots |> List.head
        
    member this.CompletedPositionCostPerShare =
        match completedPositionTransactions with
        | [] -> 0m
        | _ ->
            let totalCost = completedPositionTransactions |> List.sumBy (fun x -> x.NumberOfShares * x.Price)
            let totalShares = completedPositionTransactions |> List.sumBy _.NumberOfShares
            totalCost / totalShares
        
    member this.CompletedPositionShares = completedPositionTransactions |> List.sumBy _.NumberOfShares
        
    member this.ClosePrice =
        match liquidationSlots with
        | [] -> None
        | _ -> liquidationSlots |> List.last |> Some
        
    member this.RR =
        match this.RiskedAmount with
        | None -> 0m
        | Some riskedAmount when riskedAmount = 0m -> 0m
        | Some riskedAmount ->
            match this.Profit with
            | x when x = 0m ->
                // take the initial cost, and divide it by the risked amount
                this.Cost / riskedAmount * -1m
            | _ -> this.Profit / riskedAmount
        
    member this.FirstStop() =
        let stopSets =
            stockPosition.Transactions
            |> List.map (fun x -> match x with | Stop s -> Some s | _ -> None)
            |> List.choose id
            
        match stopSets with
        | [] -> None
        | _ -> stopSets |> List.head |> _.StopPrice
        
    member this.CostAtRiskBasedOnStopPrice =
        match this.StopPrice with
        | None -> None
        | Some stopPrice ->
            match this.IsShort with
            | false ->
                match stopPrice with
                | x when this.AverageCostPerShare < x -> Some 0m
                | _ -> (this.AverageCostPerShare - stopPrice) * this.NumberOfShares |> Some
            | true ->
                match stopPrice with
                | x when this.AverageCostPerShare > x -> Some 0m
                | _ -> (this.AverageCostPerShare - stopPrice) * this.NumberOfShares |> Some
        
    member this.PercentToStop fromPrice =
        match this.StopPrice with
            | None -> -1.0m
            | Some stopPrice ->
                match this.IsShort with
                | true -> (fromPrice - stopPrice) / stopPrice
                | false -> (stopPrice - fromPrice) / fromPrice
                
    member this.PercentToStopFromCost = this.PercentToStop this.AverageCostPerShare
    member this.GetPositionState() = stockPosition
    member this.ContainsLabel key = stockPosition.Labels.ContainsKey(key)
        
    member this.GetLabelValue key = stockPosition.Labels[key]
    member this.TryGetLabelValue key = stockPosition.TryGetLabelValue key
        
    member this.Labels = stockPosition.Labels |> Seq.map id
    
    member this.Events = events
