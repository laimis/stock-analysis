namespace di

open System.Collections.Generic
open System.Data
open System.Threading.Tasks
open core.fs
open core.Shared
open storage.shared

type IncompleteOutbox() =
    
    // TODO: will need to build proper outbox
    // this used to shuffle the events to mediatr and then to the handlers
    // but it was without any ability to handle failures etc and dependent on
    // mediatr. I might still use simple outbox that does not offer failure handling
    // but it will not be dependent on mediatr
    
    interface IOutbox with
        member _.AddEvents(events: List<AggregateEvent>, tx: IDbTransaction) =
            // Process events if needed - currently no-op
            // Original C# code had commented out event handlers
            Task.FromResult(Ok ())
