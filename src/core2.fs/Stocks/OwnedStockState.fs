namespace core.Stocks

open System
open System.Collections.Generic
open System.Linq
open core.Shared

type internal OwnedStockState() =
    let mutable _id = Guid.Empty
    let mutable _ticker: Ticker = Unchecked.defaultof<Ticker>
    let mutable _userId = Guid.Empty
    let _transactions = List<Transaction>()
    let _buyOrSell = List<IStockTransaction>()
    let _positions = List<PositionInstance>()
    let mutable _openPosition: PositionInstance = null
    let mutable _positionId = 0

    member _.Id = _id
    member _.Ticker = _ticker
    member _.UserId = _userId
    member _.Transactions = _transactions :> IList<_>
    member _.BuyOrSell = _buyOrSell :> IList<_>
    member _.OpenPosition = _openPosition
    
    member _.GetPosition(positionId: int) =
        _positions.SingleOrDefault(fun x -> x.PositionId = positionId)
    
    member _.GetClosedPositions() =
        _positions.Where(fun x -> x.IsClosed)
    
    member _.GetAllPositions() =
        _positions.AsReadOnly()

    member internal this.ApplyInternal(o: TickerObtained) =
        _id <- o.AggregateId
        _ticker <- Ticker(o.Ticker)
        _userId <- o.UserId

    [<Obsolete>]
    member internal this.ApplyInternal(c: StockCategoryChanged) = ()

    member internal this.ApplyInternal(purchased: StockPurchased) =
        this.ApplyInternal(
            StockPurchased_v2(purchased.Id, purchased.AggregateId, purchased.When, purchased.UserId, purchased.Ticker, purchased.NumberOfShares, purchased.Price, purchased.Notes, None)
        )

    member internal this.ApplyInternal(stopPriceSet: StopPriceSet) =
        _openPosition.SetStopPrice(Some stopPriceSet.StopPrice, stopPriceSet.When)

    member internal this.ApplyInternal(deleted: StopDeleted) =
        _openPosition.DeleteStopPrice(deleted.When)

    member internal this.ApplyInternal(riskAmountSet: RiskAmountSet) =
        _openPosition.SetRiskAmount(riskAmountSet.RiskAmount, riskAmountSet.When)

    member internal this.ApplyInternal(gradeAssigned: TradeGradeAssigned) =
        let position = _positions.Single(fun x -> x.PositionId = gradeAssigned.PositionId)
        position.SetGrade(TradeGrade(gradeAssigned.Grade), gradeAssigned.When, gradeAssigned.Note)

    member internal this.ApplyInternal(riskAmountSet: PositionRiskAmountSet) =
        let position = _positions.Single(fun x -> x.PositionId = riskAmountSet.PositionId)
        position.SetRiskAmount(riskAmountSet.RiskAmount, riskAmountSet.When)

    member internal this.ApplyInternal(purchased: StockPurchased_v2) =
        _buyOrSell.Add(purchased)

        if isNull _openPosition then
            _openPosition <- PositionInstance(_positionId, Ticker(purchased.Ticker), purchased.When)
            _positions.Add(_openPosition)
            _positionId <- _positionId + 1

        _openPosition.Buy(numberOfShares = purchased.NumberOfShares, price = purchased.Price, transactionId = purchased.Id, ``when`` = purchased.When, notes = purchased.Notes)

        if purchased.StopPrice.IsSome then
            _openPosition.SetStopPrice(purchased.StopPrice, purchased.When)

        _transactions.Add(
            Transaction.NonPLTx(
                _id,
                purchased.Id,
                _ticker,
                sprintf "Purchased %M shares @ $%M/share" purchased.NumberOfShares purchased.Price,
                purchased.Price,
                -purchased.Price * purchased.NumberOfShares,
                purchased.When,
                isOption = false
            )
        )

    member internal this.ApplyInternal(deleted: StockDeleted) =
        _openPosition <- null
        _buyOrSell.Clear()
        _positions.Clear()
        _transactions.Clear()

    member internal this.ApplyInternal(deleted: StockTransactionDeleted) =
        if _positions.Count = 0 then
            raise (InvalidOperationException("Cannot delete a transaction from a stock that has no position"))

        let last = _buyOrSell.Single(fun t -> t.Id = deleted.TransactionId)
        _buyOrSell.Remove(last) |> ignore

        let transaction = _transactions.Single(fun t -> t.EventId = deleted.TransactionId)
        _transactions.Remove(transaction) |> ignore

        let lastPosition = _positions.Last()
        lastPosition.RemoveTransaction(deleted.TransactionId)

        if lastPosition.NumberOfShares = 0m then
            _openPosition <- null
            _positions.Remove(lastPosition) |> ignore
        else
            _openPosition <- lastPosition

    member internal this.ApplyInternal(deleted: PositionDeleted) =
        let position = _positions.Single(fun x -> x.PositionId = deleted.PositionId)

        let transactionsToRemove = position.Transactions.Select(fun x -> x.TransactionId).ToList()
        for transactionId in transactionsToRemove do
            let transaction = _transactions.Single(fun x -> x.EventId = transactionId)
            _transactions.Remove(transaction) |> ignore

            let buyOrSell = _buyOrSell.Single(fun x -> x.Id = transactionId)
            _buyOrSell.Remove(buyOrSell) |> ignore

        _positions.Remove(position) |> ignore
        if obj.ReferenceEquals(position, _openPosition) then
            _openPosition <- null

    member internal this.ApplyInternal(notesAdded: NotesAdded) =
        let position = _positions.SingleOrDefault(fun x -> x.PositionId = notesAdded.PositionId)
        if not (isNull position) then
            position.AddNotes(notesAdded.Notes)

    member internal this.ApplyInternal(sold: StockSold) =
        _buyOrSell.Add(sold)

        if isNull _openPosition then
            raise (InvalidOperationException("Cannot sell stock that is not owned"))

        let profitBefore = _openPosition.Profit

        _openPosition.Sell(
            numberOfShares = sold.NumberOfShares,
            price = sold.Price,
            transactionId = sold.Id,
            ``when`` = sold.When,
            notes = sold.Notes
        )

        let profitAfter = _openPosition.Profit

        _transactions.Add(
            Transaction.NonPLTx(
                _id,
                sold.Id,
                _ticker,
                sprintf "Sold %M shares @ $%M/share" sold.NumberOfShares sold.Price,
                sold.Price,
                sold.Price * sold.NumberOfShares,
                sold.When,
                isOption = false
            )
        )

        _transactions.Add(
            Transaction.PLTx(
                _id,
                _ticker,
                sprintf "Sold %M shares @ $%M/share" sold.NumberOfShares sold.Price,
                sold.Price,
                amount = profitAfter - profitBefore,
                ``when`` = sold.When,
                isOption = false
            )
        )

        if _openPosition.NumberOfShares = 0m then
            _openPosition <- null

    member private this.ApplyInternal(labelSet: PositionLabelSet) =
        let position = _positions.Single(fun x -> x.PositionId = labelSet.PositionId)
        position.SetLabel(labelSet)

    member private this.ApplyInternal(labelDeleted: PositionLabelDeleted) =
        let position = _positions.Single(fun x -> x.PositionId = labelDeleted.PositionId)
        position.DeleteLabel(labelDeleted)

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? TickerObtained as e -> this.ApplyInternal(e)
        | :? StockCategoryChanged as e -> this.ApplyInternal(e)
        | :? StockPurchased as e -> this.ApplyInternal(e)
        | :? StockPurchased_v2 as e -> this.ApplyInternal(e)
        | :? StopPriceSet as e -> this.ApplyInternal(e)
        | :? StopDeleted as e -> this.ApplyInternal(e)
        | :? RiskAmountSet as e -> this.ApplyInternal(e)
        | :? TradeGradeAssigned as e -> this.ApplyInternal(e)
        | :? PositionRiskAmountSet as e -> this.ApplyInternal(e)
        | :? StockDeleted as e -> this.ApplyInternal(e)
        | :? StockTransactionDeleted as e -> this.ApplyInternal(e)
        | :? PositionDeleted as e -> this.ApplyInternal(e)
        | :? NotesAdded as e -> this.ApplyInternal(e)
        | :? StockSold as e -> this.ApplyInternal(e)
        | :? PositionLabelSet as e -> this.ApplyInternal(e)
        | :? PositionLabelDeleted as e -> this.ApplyInternal(e)
        | _ -> ()

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)
