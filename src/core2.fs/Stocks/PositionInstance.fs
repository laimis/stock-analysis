namespace core.Stocks

open System
open System.Collections.Generic
open System.Linq
open core.Shared

type PositionEventType(value: string) =
    static member Buy = "buy"
    static member Stop = "stop"
    static member Sell = "sell"
    static member Risk = "risk"
    static member Label = "label"

    member _.Value =
        match value with
        | "buy" -> PositionEventType.Buy
        | "stop" -> PositionEventType.Stop
        | "sell" -> PositionEventType.Sell
        | "risk" -> PositionEventType.Risk
        | "label" -> PositionEventType.Label
        | _ -> raise (InvalidOperationException(sprintf "Invalid position event type: %s" value))

[<Struct>]
type PositionEvent =
    { Id: Guid
      Description: string
      Type: PositionEventType
      Value: decimal option
      When: DateTimeOffset
      Quantity: decimal option
      Notes: string }
    member this.Date = this.When.ToString("yyyy-MM-dd")

[<Struct>]
type PositionTransaction =
    { NumberOfShares: decimal
      Price: decimal
      TransactionId: Guid
      Type: string
      When: DateTimeOffset }
    member this.Date = this.When.ToString("yyyy-MM-dd")
    member this.AgeInDays = int ((DateTimeOffset.UtcNow - this.When).TotalDays)

[<AllowNullLiteral>]
type PositionInstance(positionId: int, ticker: Ticker, opened: DateTimeOffset) =
    let mutable _numberOfShares = 0m
    let mutable _averageCostPerShare = 0m
    let mutable _averageSaleCostPerShare = 0m
    let mutable _averageBuyCostPerShare = 0m
    let mutable _cost = 0m
    let mutable _profit = 0m
    let mutable _closed: DateTimeOffset option = None
    let mutable _firstStop: decimal option = None
    let mutable _riskedAmount: decimal option = None
    let mutable _stopPrice: decimal option = None
    let mutable _lastTransaction = DateTimeOffset.MinValue
    let mutable _lastSellPrice = 0m
    let mutable _positionCompleted = false
    let mutable _completedPositionCost = 0m
    let mutable _completedPositionShares = 0m
    let mutable _grade: TradeGrade option = None
    let mutable _gradeDate: DateTimeOffset option = None
    let mutable _gradeNote: string = null
    let _slots = List<decimal>()
    let _transactions = List<PositionTransaction>()
    let _events = List<PositionEvent>()
    let _notes = List<string>()
    let _labels = Dictionary<string, string>()

    member _.NumberOfShares = _numberOfShares
    member _.AverageCostPerShare = _averageCostPerShare
    member _.AverageSaleCostPerShare = _averageSaleCostPerShare
    member _.AverageBuyCostPerShare = _averageBuyCostPerShare
    member _.Opened = opened
    member this.DaysHeld = 
        int ((match _closed with Some c -> c | None -> DateTimeOffset.UtcNow).Subtract(opened).TotalDays)
    member _.Cost = _cost
    member _.Profit = _profit
    member this.GainPct =
        match _averageSaleCostPerShare with
        | 0m -> 0m
        | _ -> (_averageSaleCostPerShare - _averageBuyCostPerShare) / _averageBuyCostPerShare
    member this.RR =
        match _riskedAmount with
        | Some ra when ra <> 0m -> _profit / ra
        | _ -> 0m
    member this.RRWeighted = this.RR * _cost
    member _.IsClosed = _closed.IsSome
    member _.PositionId = positionId
    member _.Ticker = ticker
    member _.Closed = _closed
    member _.FirstStop = _firstStop
    member _.RiskedAmount = _riskedAmount
    member this.CostAtRiskedBasedOnStopPrice =
        match _stopPrice with
        | Some sp when sp > _averageCostPerShare -> Some 0m
        | Some sp -> Some ((_averageCostPerShare - sp) * _numberOfShares)
        | None -> None
    member _.Transactions = _transactions :> IList<_>
    member _.Events = _events :> IList<_>
    member _.StopPrice = _stopPrice
    member _.LastTransaction = _lastTransaction
    member _.LastSellPrice = _lastSellPrice
    member this.DaysSinceLastTransaction = int ((DateTimeOffset.UtcNow - _lastTransaction).TotalDays)
    member _.CompletedPositionCost = _completedPositionCost
    member _.CompletedPositionShares = _completedPositionShares
    member this.CompletedPositionCostPerShare = _completedPositionCost / _completedPositionShares
    member _.Grade = _grade
    member _.GradeDate = _gradeDate
    member _.GradeNote = _gradeNote
    member _.Notes = _notes :> IList<_>
    member _.Labels = _labels :> IEnumerable<KeyValuePair<string, string>>

    member this.SetGrade(grade: TradeGrade, ``when``: DateTimeOffset, ?note: string) =
        _grade <- Some grade
        let previousNote = _gradeNote
        _gradeNote <- defaultArg note null
        _gradeDate <- Some ``when``
        _notes.Remove(previousNote) |> ignore
        if note.IsSome then _notes.Add(note.Value)

    member _.AddNotes(notes: string) =
        _notes.Add(notes)

    member private this.RunCalculations() =
        _slots.Clear()
        let mutable cost = 0m
        let mutable profit = 0m
        let mutable numberOfShares = 0m
        let mutable totalSale = 0m
        let mutable totalNumberOfSharesSold = 0m
        let mutable totalBuy = 0m
        let mutable totalNumberOfSharesBought = 0m

        for transaction in _transactions do
            if transaction.Type = "buy" then
                for i in 0 .. int transaction.NumberOfShares - 1 do
                    _slots.Add(transaction.Price)
                    cost <- cost + transaction.Price
                    numberOfShares <- numberOfShares + 1m
                totalBuy <- totalBuy + transaction.Price * transaction.NumberOfShares
                totalNumberOfSharesBought <- totalNumberOfSharesBought + transaction.NumberOfShares
            else
                let removed = _slots.Take(int transaction.NumberOfShares).ToList()
                _slots.RemoveRange(0, int transaction.NumberOfShares)
                for removedElement in removed do
                    profit <- profit + transaction.Price - removedElement
                    cost <- cost - removedElement
                    numberOfShares <- numberOfShares - 1m
                totalSale <- totalSale + transaction.Price * transaction.NumberOfShares
                totalNumberOfSharesSold <- totalNumberOfSharesSold + transaction.NumberOfShares

        _averageCostPerShare <-
            match _slots.Count with
            | 0 -> 0m
            | _ -> _slots.Sum() / decimal _slots.Count
        _cost <- cost
        _profit <- profit
        _numberOfShares <- numberOfShares
        _averageSaleCostPerShare <-
            match totalNumberOfSharesSold with
            | 0m -> 0m
            | _ -> totalSale / totalNumberOfSharesSold
        _averageBuyCostPerShare <-
            match totalNumberOfSharesBought with
            | 0m -> 0m
            | _ -> totalBuy / totalNumberOfSharesBought

    member this.Buy(numberOfShares: decimal, price: decimal, ``when``: DateTimeOffset, transactionId: Guid, ?notes: string) =
        _transactions.Add({ NumberOfShares = numberOfShares; Price = price; TransactionId = transactionId; Type = "buy"; When = ``when`` })
        _events.Add({ Id = transactionId; Description = sprintf "buy %M @ $%M" numberOfShares price; Type = PositionEventType(PositionEventType.Buy); Value = Some price; When = ``when``; Quantity = Some numberOfShares; Notes = defaultArg notes null })

        if not _positionCompleted then
            _completedPositionCost <- _completedPositionCost + price * numberOfShares
            _completedPositionShares <- _completedPositionShares + numberOfShares

        if notes.IsSome then _notes.Add(notes.Value)
        _lastTransaction <- ``when``
        this.RunCalculations()

    member internal this.RemoveTransaction(transactionId: Guid) =
        let tx = _transactions.SingleOrDefault(fun t -> t.TransactionId = transactionId)
        if tx.TransactionId = Guid.Empty then
            raise (InvalidOperationException(sprintf "Transaction %A not found" transactionId))
        _transactions.Remove(tx) |> ignore

        let ev = _events.SingleOrDefault(fun e -> e.Id = transactionId)
        if ev.Id = Guid.Empty then
            raise (InvalidOperationException(sprintf "Event %A not found" transactionId))
        _events.Remove(ev) |> ignore

        this.RunCalculations()

    member private this.HasEventWithDescription(testDescription: string) =
        _events.Any(fun e -> e.Description = testDescription)

    member this.Sell(numberOfShares: decimal, price: decimal, transactionId: Guid, ``when``: DateTimeOffset, ?notes: string) =
        if _numberOfShares - numberOfShares < 0m then
            let details = sprintf "Sell %M @ $%M on %O for %O" numberOfShares price ``when`` ticker
            raise (InvalidOperationException("Transaction would make amount owned invalid: " + details))

        if not _positionCompleted then
            _positionCompleted <- true

        let percentGainAtSale = (price - _averageCostPerShare) / _averageCostPerShare
        _transactions.Add({ NumberOfShares = numberOfShares; Price = price; TransactionId = transactionId; Type = "sell"; When = ``when`` })
        _events.Add({ Id = transactionId; Description = sprintf "sell %M @ $%M (%.2f%%)" numberOfShares price (float (percentGainAtSale * 100m)); Type = PositionEventType(PositionEventType.Sell); Value = Some price; When = ``when``.DateTime; Quantity = Some numberOfShares; Notes = defaultArg notes null })

        if _stopPrice.IsNone && not (this.HasEventWithDescription("Stop price deleted")) then
            this.SetStopPrice(Some (this.CompletedPositionCostPerShare * 0.95m), ``when``)

        if notes.IsSome then _notes.Add(notes.Value)
        _lastTransaction <- ``when``
        _lastSellPrice <- price
        this.RunCalculations()

        if _numberOfShares = 0m then
            _closed <- Some ``when``

    member this.SetStopPrice(stopPrice: decimal option, ``when``: DateTimeOffset) =
        match stopPrice with
        | Some sp when Some sp <> _stopPrice ->
            _stopPrice <- Some sp
            if _firstStop.IsNone then _firstStop <- Some sp
            let stopPercentage = (sp - _averageCostPerShare) / _averageCostPerShare
            _events.Add({ Id = Guid.Empty; Description = sprintf "Stop price set to %.2f (%.1f%%)" sp (stopPercentage * 100m); Type = PositionEventType(PositionEventType.Stop); Value = Some sp; When = ``when``; Quantity = None; Notes = null })
            if _riskedAmount.IsNone then
                this.SetRiskAmount((_averageCostPerShare - sp) * _numberOfShares, ``when``)
        | _ -> ()

    member this.DeleteStopPrice(``when``: DateTimeOffset) =
        _stopPrice <- None
        _riskedAmount <- None
        _events.Add({ Id = Guid.Empty; Description = "Stop price deleted"; Type = PositionEventType(PositionEventType.Stop); Value = None; When = ``when``; Quantity = None; Notes = null })

    member this.SetRiskAmount(riskAmount: decimal, ``when``: DateTimeOffset) =
        if riskAmount <> 0m then
            _riskedAmount <- Some riskAmount
            _events.Add({ Id = Guid.Empty; Description = sprintf "Set risk amount to %.2f" riskAmount; Type = PositionEventType(PositionEventType.Risk); Value = Some riskAmount; When = ``when``; Quantity = None; Notes = null })

    member this.ContainsLabel(key: string, value: string) =
        match _labels.TryGetValue(key) with
        | true, v when v = value -> true
        | _ -> false

    member internal this.SetLabel(labelSet: PositionLabelSet) =
        _labels.[labelSet.Key] <- labelSet.Value
        _events.Add({ Id = Guid.NewGuid(); Description = labelSet.Key; Type = PositionEventType(PositionEventType.Label); Value = None; When = labelSet.When; Quantity = None; Notes = labelSet.Value })

    member this.ContainsLabel(key: string) =
        _labels.ContainsKey(key)

    member internal this.DeleteLabel(labelDeleted: PositionLabelDeleted) =
        _labels.Remove(labelDeleted.Key) |> ignore
        _events.Add({ Id = Guid.NewGuid(); Description = labelDeleted.Key; Type = PositionEventType(PositionEventType.Label); Value = None; When = labelDeleted.When; Quantity = None; Notes = null })

    member this.GetLabelValue(key: string) = _labels.[key]
