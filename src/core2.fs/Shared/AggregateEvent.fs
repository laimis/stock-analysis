namespace core.Shared

open System

[<AllowNullLiteral>]
type AggregateEvent(id: Guid, aggregateId: Guid, ``when``: DateTimeOffset) =
    do
        if id = Guid.Empty then
            invalidOp "id cannot be empty"
        if aggregateId = Guid.Empty then
            invalidOp "aggregateId cannot be empty"
    
    member val Id = id with get
    member val AggregateId = aggregateId with get
    member val When = ``when`` with get
