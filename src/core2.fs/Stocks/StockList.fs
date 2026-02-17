namespace core.Stocks

open System
open System.Collections.Generic
open core.Shared

[<Struct>]
type StockListTicker =
    { Note: string
      Ticker: Ticker
      When: DateTimeOffset }

type StockListState() =
    let mutable _id = Guid.Empty
    let mutable _userId = Guid.Empty
    let mutable _name: string = null
    let mutable _description: string = null
    let _tickers = List<StockListTicker>()
    let _tags = HashSet<string>()

    member _.Id = _id
    member _.UserId = _userId
    member _.Name = _name
    member _.Description = _description
    member _.Tickers = _tickers :> IList<_>
    member _.Tags = _tags :> HashSet<_>

    member this.Apply(e: AggregateEvent) =
        this.ApplyInternal(e :> obj)

    member _.ContainsTag(tag: string) = _tags.Contains(tag)

    member private this.ApplyInternal(obj: obj) =
        match obj with
        | :? StockListCreated as e -> this.ApplyInternal(e)
        | :? StockListUpdated as e -> this.ApplyInternal(e)
        | :? StockListTickerAdded as e -> this.ApplyInternal(e)
        | :? StockListTickerRemoved as e -> this.ApplyInternal(e)
        | :? StockListTagAdded as e -> this.ApplyInternal(e)
        | :? StockListTagRemoved as e -> this.ApplyInternal(e)
        | :? StockListCleared as e -> this.ApplyInternal(e)
        | _ -> ()

    member private this.ApplyInternal(created: StockListCreated) =
        _description <- created.Description
        _id <- created.AggregateId
        _name <- created.Name
        _userId <- created.UserId

    member private this.ApplyInternal(updated: StockListUpdated) =
        _description <- updated.Description
        _name <- updated.Name

    member private this.ApplyInternal(added: StockListTickerAdded) =
        _tickers.Add({ Note = added.Note; Ticker = Ticker(added.Ticker); When = added.When })

    member private this.ApplyInternal(removed: StockListTickerRemoved) =
        _tickers.RemoveAll(fun x -> x.Ticker.Value = removed.Ticker) |> ignore

    member private this.ApplyInternal(added: StockListTagAdded) =
        _tags.Add(added.Tag) |> ignore

    member private this.ApplyInternal(removed: StockListTagRemoved) =
        _tags.Remove(removed.Tag) |> ignore

    member private this.ApplyInternal(cleared: StockListCleared) =
        _tickers.Clear()

    interface IAggregateState with
        member this.Id = this.Id
        member this.Apply(e) = this.Apply(e)

type StockList =
    inherit Aggregate<StockListState>

    new (events: IEnumerable<AggregateEvent>) = { inherit Aggregate<StockListState>(events) }

    new (description: string, name: string, userId: Guid) as this =
        { inherit Aggregate<StockListState>() }
        then
            if userId = Guid.Empty then
                raise (InvalidOperationException("Missing user id"))

            if String.IsNullOrWhiteSpace(name) then
                raise (InvalidOperationException("Missing list name"))

            this.Apply(StockListCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, description, name, userId))

    member this.Update(name: string, description: string) =
        if String.IsNullOrWhiteSpace(name) then
            raise (InvalidOperationException("Missing name"))

        if this.State.Name = name && this.State.Description = description then
            ()
        else
            this.Apply(StockListUpdated(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, description, name))

    member this.AddStock(ticker: Ticker, note: string) =
        let exists = this.State.Tickers |> Seq.exists (fun x -> x.Ticker.Equals(ticker))
        if not exists then
            this.Apply(StockListTickerAdded(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, note, ticker.Value))

    member this.RemoveStock(ticker: Ticker) =
        let exists = this.State.Tickers |> Seq.exists (fun x -> x.Ticker.Equals(ticker))
        if not exists then
            raise (InvalidOperationException("Ticker does not exist in the list"))

        this.Apply(StockListTickerRemoved(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, ticker.Value))

    member this.AddTag(tag: string) =
        if String.IsNullOrWhiteSpace(tag) then
            raise (InvalidOperationException("Missing tag"))

        if not (this.State.ContainsTag(tag)) then
            this.Apply(StockListTagAdded(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, tag))

    member this.RemoveTag(tag: string) =
        if String.IsNullOrWhiteSpace(tag) then
            raise (InvalidOperationException("Missing tag"))

        if this.State.ContainsTag(tag) then
            this.Apply(StockListTagRemoved(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, tag))

    member this.Clear() =
        if this.State.Tickers.Count > 0 then
            this.Apply(StockListCleared(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow))
