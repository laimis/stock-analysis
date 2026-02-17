namespace core.Shared

open System
open System.Collections.Generic

type IAggregate =
    abstract member Version: int with get
    abstract member Events: IEnumerable<AggregateEvent> with get

[<AbstractClass>]
type Aggregate<'T when 'T : (new : unit -> 'T) and 'T :> IAggregateState>() =
    let _aggregateState = new 'T()
    let _events = new List<AggregateEvent>()
    let mutable _version = 0
    
    member private this.ApplyInternal(e: AggregateEvent) =
        _events.Add(e)
        _aggregateState.Apply(e)
        _version <- _version + 1
    
    member this.Apply(e: AggregateEvent) =
        _events.Add(e)
        _aggregateState.Apply(e)
    
    member this.State = _aggregateState
    member this.Events : IEnumerable<AggregateEvent> = _events :> IEnumerable<AggregateEvent>
    member this.Version = _version
    member this.Id = _aggregateState.Id
    
    new(events: IEnumerable<AggregateEvent>) as this =
        Aggregate<'T>()
        then
            for e in events do
                this.ApplyInternal(e)
    
    interface IAggregate with
        member this.Version = this.Version
        member this.Events = this.Events
