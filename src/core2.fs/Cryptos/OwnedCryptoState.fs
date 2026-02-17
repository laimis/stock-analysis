namespace core.Cryptos

open System
open System.Collections.Generic
open System.Linq
open core.Shared

type OwnedCryptoState() =
    let mutable _id = Guid.Empty
    let mutable _token: string = null
    let mutable _userId = Guid.Empty
    let mutable _quantity = 0m
    let mutable _cost = 0m
    let _transactions = List<CryptoTransaction>()
    let _buyOrSell = List<ICryptoTransaction>()
    let _deletes = HashSet<Guid>()
    let _positionInstances = List<PositionInstance>()
    let _awards = List<CryptoAwarded>()
    let _yields = List<CryptoYielded>()
    let mutable _daysHeld = 0
    let mutable _daysSinceLastTransaction = 0

    member _.Id = _id
    member _.Token = _token
    member _.UserId = _userId
    member _.Quantity = _quantity
    member _.Cost = _cost
    member this.AverageCost = _cost / _quantity
    member _.Transactions = _transactions :> IList<_>
    member internal _.BuyOrSell = _buyOrSell :> IList<_>
    member internal _.Deletes = _deletes :> HashSet<_>
    member _.PositionInstances = _positionInstances :> IList<_>
    member this.Description = sprintf "%M tokens at avg cost %M" _quantity (Math.Round(this.AverageCost, 2))
    member _.DaysHeld = _daysHeld
    member _.DaysSinceLastTransaction = _daysSinceLastTransaction
    member this.UndeletedBuysOrSells =
        _buyOrSell.Where(fun a -> not (_deletes.Contains(a.Id))).Cast<AggregateEvent>()
    member _.Awards = _awards :> IList<_>
    member _.Yields = _yields :> IList<_>

    member internal this.ApplyInternal(o: CryptoObtained) =
        _id <- o.AggregateId
        _token <- o.Token
        _userId <- o.UserId

    member internal this.ApplyInternal(purchased: CryptoPurchased) =
        _buyOrSell.Add(purchased)
        this.StateUpdateLoop()

    member internal this.ApplyInternal(deleted: CryptoDeleted) =
        for t in _buyOrSell do
            _deletes.Add(t.Id) |> ignore
        this.StateUpdateLoop()

    member internal this.ApplyInternal(deleted: CryptoTransactionDeleted) =
        _deletes.Add(deleted.TransactionId) |> ignore
        this.StateUpdateLoop()

    member internal this.ApplyInternal(sold: CryptoSold) =
        _buyOrSell.Add(sold)
        this.StateUpdateLoop()

    member internal this.ApplyInternal(awarded: CryptoAwarded) =
        _awards.Add(awarded)
        this.StateUpdateLoop()

    member internal this.ApplyInternal(yielded: CryptoYielded) =
        _yields.Add(yielded)
        this.StateUpdateLoop()

    member private this.StateUpdateLoop() =
        let mutable quantity = 0m
        let mutable cost = 0m
        let txs = List<CryptoTransaction>()
        let mutable oldestOpen: DateTimeOffset option = None
        let positionInstances = List<PositionInstance>()
        let mutable lastTransaction = DateTimeOffset.UtcNow

        let purchaseProcessing (st: ICryptoTransaction) =
            lastTransaction <- st.When

            if quantity = 0m then
                oldestOpen <- Some st.When
                positionInstances.Add(PositionInstance(_token))

            quantity <- quantity + st.Quantity
            cost <- cost + st.DollarAmount

            txs.Add(
                CryptoTransaction.DebitTx(
                    _id,
                    st.Id,
                    _token,
                    sprintf "Purchased %M for $%M" st.Quantity st.DollarAmount,
                    st.DollarAmount / st.Quantity,
                    st.DollarAmount,
                    st.When
                )
            )

            positionInstances.[positionInstances.Count - 1].Buy(st.Quantity, st.DollarAmount, st.When)
            true

        let sellProcessing (st: ICryptoTransaction) =
            if positionInstances.Count > 0 then
                positionInstances.[positionInstances.Count - 1].Sell(st.Quantity, st.DollarAmount, st.When)

            lastTransaction <- st.When

            txs.Add(
                CryptoTransaction.CreditTx(
                    _id,
                    st.Id,
                    _token,
                    sprintf "Sold %M for $%M" st.Quantity st.DollarAmount,
                    st.DollarAmount / st.Quantity,
                    st.DollarAmount,
                    st.When
                )
            )

            quantity <- quantity - st.Quantity
            cost <- cost - st.DollarAmount
            true

        for st in _buyOrSell.OrderBy(fun e -> e.When).ThenBy(fun i -> _buyOrSell.IndexOf(i)) do
            if not (_deletes.Contains(st.Id)) then
                match st with
                | :? CryptoPurchased as sp -> purchaseProcessing sp |> ignore
                | :? CryptoSold as ss -> sellProcessing ss |> ignore
                | _ -> ()

                if quantity = 0m then
                    cost <- 0m
                    oldestOpen <- None

        for a in _awards do
            if not (_deletes.Contains(a.Id)) then
                if quantity = 0m then
                    oldestOpen <- Some a.When
                quantity <- quantity + a.Quantity

        for y in _yields do
            if not (_deletes.Contains(y.Id)) then
                quantity <- quantity + y.Quantity

        _quantity <- quantity
        _cost <- cost
        _transactions.Clear()
        _transactions.AddRange(txs)
        _positionInstances.Clear()
        _positionInstances.AddRange(positionInstances)

        _daysHeld <-
            match oldestOpen with
            | Some oo -> int (Math.Floor(DateTimeOffset.UtcNow.Subtract(oo).TotalDays))
            | None -> 0

        _daysSinceLastTransaction <- int (DateTimeOffset.UtcNow.Subtract(lastTransaction).TotalDays)

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? CryptoObtained as e -> this.ApplyInternal(e)
        | :? CryptoPurchased as e -> this.ApplyInternal(e)
        | :? CryptoDeleted as e -> this.ApplyInternal(e)
        | :? CryptoTransactionDeleted as e -> this.ApplyInternal(e)
        | :? CryptoSold as e -> this.ApplyInternal(e)
        | :? CryptoAwarded as e -> this.ApplyInternal(e)
        | :? CryptoYielded as e -> this.ApplyInternal(e)
        | _ -> ()

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)
