namespace core.Stocks

open System
open System.Collections.Generic
open System.Linq
open core.Shared

type internal OwnedStock =
    inherit Aggregate<OwnedStockState>

    new (events: IEnumerable<AggregateEvent>) = { inherit Aggregate<OwnedStockState>(events) }

    new (ticker: Ticker, userId: Guid) as this =
        { inherit Aggregate<OwnedStockState>() }
        then
            if userId = Guid.Empty then
                raise (InvalidOperationException("Missing user id"))
            this.Apply(TickerObtained(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, ticker.Value, userId))

    member this.Purchase(numberOfShares: decimal, price: decimal, date: DateTimeOffset, ?notes: string, ?stopPrice: decimal) =
        if price <= 0m then
            raise (InvalidOperationException("Price cannot be negative or zero"))

        if date.Subtract(DateTimeOffset.UtcNow).TotalHours >= 12.0 then
            raise (InvalidOperationException("Purchase date cannot be in the future"))

        if date < DateTimeOffset.UnixEpoch then
            raise (InvalidOperationException("Purchase date cannot be before 1970"))

        this.Apply(
            StockPurchased_v2(
                Guid.NewGuid(),
                this.State.Id,
                date,
                this.State.UserId,
                this.State.Ticker.Value,
                numberOfShares,
                price,
                defaultArg notes null,
                stopPrice
            )
        )

    member this.DeleteStop() =
        if isNull this.State.OpenPosition then
            raise (InvalidOperationException("No open position to delete stop for"))

        this.Apply(
            StopDeleted(
                Guid.NewGuid(),
                this.State.Id,
                DateTimeOffset.UtcNow,
                this.State.UserId,
                this.State.Ticker.Value
            )
        )

    member this.SetRiskAmount(riskAmount: decimal, positionId: int) =
        let position = this.State.GetPosition(positionId)
        if isNull position then
            raise (InvalidOperationException("Unable to find position with id " + string positionId))

        if riskAmount < 0m then
            raise (InvalidOperationException("Risk amount cannot be negative"))

        this.Apply(PositionRiskAmountSet(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, this.State.UserId, positionId, riskAmount))

    member this.SetStop(stopPrice: decimal) =
        if isNull this.State.OpenPosition then
            raise (InvalidOperationException("No open position to set stop on"))

        if stopPrice < 0m then
            raise (InvalidOperationException("Stop price cannot be negative"))

        if Some stopPrice = this.State.OpenPosition.StopPrice then
            false
        else
            this.Apply(
                StopPriceSet(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, this.State.UserId, this.State.Ticker.Value, stopPrice)
            )
            true

    member this.AddNotes(notes: string) =
        if isNull this.State.OpenPosition then
            raise (InvalidOperationException("No open position to add notes to"))

        if String.IsNullOrEmpty(notes) then
            false
        elif this.State.OpenPosition.Notes.Contains(notes) then
            false
        else
            this.Apply(
                NotesAdded(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    this.State.OpenPosition.PositionId,
                    notes
                )
            )
            true

    member this.DeleteTransaction(transactionId: Guid) =
        if this.State.BuyOrSell.All(fun t -> t.Id <> transactionId) then
            raise (InvalidOperationException("Unable to find transaction to delete using id " + string transactionId))

        this.Apply(
            StockTransactionDeleted(
                Guid.NewGuid(),
                this.State.Id,
                transactionId,
                DateTimeOffset.UtcNow
            )
        )

    member this.Delete() =
        this.Apply(
            StockDeleted(
                Guid.NewGuid(),
                this.State.Id,
                DateTimeOffset.UtcNow
            )
        )

    member this.Sell(numberOfShares: decimal, price: decimal, date: DateTimeOffset, notes: string) =
        if isNull this.State.OpenPosition then
            raise (InvalidOperationException("No open position to sell"))

        if this.State.OpenPosition.NumberOfShares < numberOfShares then
            raise (InvalidOperationException("Cannot sell more shares than owned"))

        if price < 0m then
            raise (InvalidOperationException("Price cannot be negative or zero"))

        this.Apply(
            StockSold(
                Guid.NewGuid(),
                this.State.Id,
                date,
                this.State.UserId,
                this.State.Ticker.Value,
                numberOfShares,
                price,
                notes
            )
        )

    member this.AssignGrade(positionId: int, grade: TradeGrade, note: string) =
        let position = this.State.GetPosition(positionId)
        if isNull position then
            raise (InvalidOperationException("Unable to find position with id " + string positionId))

        if not position.IsClosed then
            raise (InvalidOperationException("Cannot assign grade to an open position"))

        if position.Grade = Some grade && position.GradeNote = note then
            false
        else
            this.Apply(
                TradeGradeAssigned(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    userId = this.State.UserId,
                    grade = grade.Value,
                    note = note,
                    positionId = positionId
                )
            )
            true

    member this.DeletePosition(positionId: int) =
        let position = this.State.GetPosition(positionId)
        if isNull position then
            false
        elif position.IsClosed then
            raise (InvalidOperationException("Cannot delete a closed position"))
        else
            this.Apply(
                PositionDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    positionId
                )
            )
            true

    member this.SetPositionLabel(positionId: int, key: string, value: string) =
        let position = this.State.GetPosition(positionId)
        if isNull position then
            raise (InvalidOperationException("Unable to find position with id " + string positionId))

        if String.IsNullOrEmpty(key) then
            raise (InvalidOperationException("Key cannot be empty"))

        if String.IsNullOrEmpty(value) then
            raise (InvalidOperationException("Value cannot be empty"))

        if position.ContainsLabel(key, value) then
            false
        else
            this.Apply(
                PositionLabelSet(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    positionId,
                    key,
                    value
                )
            )
            true

    member this.DeletePositionLabel(positionId: int, key: string) =
        let position = this.State.GetPosition(positionId)
        if isNull position then
            raise (InvalidOperationException("Unable to find position with id " + string positionId))

        if String.IsNullOrEmpty(key) then
            raise (InvalidOperationException("Key cannot be empty"))

        if not (position.ContainsLabel(key)) then
            false
        else
            this.Apply(
                PositionLabelDeleted(
                    Guid.NewGuid(),
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    positionId,
                    key
                )
            )
            true
