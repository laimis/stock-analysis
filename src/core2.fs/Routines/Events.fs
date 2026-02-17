namespace core.Routines

open System
open core.Shared

[<AllowNullLiteral>]
type internal RoutineCreated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, description: string, name: string, userId: Guid) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Description = description with get
    member val Name = name with get
    member val UserId = userId with get

[<AllowNullLiteral>]
type internal RoutineUpdated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, description: string, name: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Description = description with get
    member val Name = name with get

[<AllowNullLiteral>]
type internal RoutineStepAdded(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, label: string, url: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Label = label with get
    member val Url = url with get

[<AllowNullLiteral>]
type internal RoutineStepRemoved(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, index: int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Index = index with get

[<AllowNullLiteral>]
type internal RoutineStepMoved(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, direction: int, stepIndex: int) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Direction = direction with get
    member val StepIndex = stepIndex with get

[<AllowNullLiteral>]
type internal RoutineStepUpdated(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset, index: int, label: string, url: string) =
    inherit AggregateEvent(id, aggregateId, ``when``)
    member val Index = index with get
    member val Label = label with get
    member val Url = url with get
