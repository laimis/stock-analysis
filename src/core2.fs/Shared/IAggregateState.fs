namespace core.Shared

open System

type IAggregateState =
    abstract member Id: Guid with get
    abstract member Apply: AggregateEvent -> unit
