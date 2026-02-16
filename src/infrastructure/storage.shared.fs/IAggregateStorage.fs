namespace storage.shared

open System
open System.Collections.Generic
open System.Data
open System.Threading.Tasks
open core.fs.Accounts
open core.Shared

type IAggregateStorage =
    abstract member GetEventsAsync : entity:string * userId:UserId -> Task<IEnumerable<AggregateEvent>>
    abstract member GetEventsAsync : entity:string * aggregateId:Guid * userId:UserId -> Task<IEnumerable<AggregateEvent>>
    abstract member SaveEventsAsync : agg:IAggregate * entity:string * userId:UserId * outsideTransaction:IDbTransaction -> Task
    abstract member SaveEventsAsync : old:IAggregate * newAggregate:IAggregate * entity:string * userId:UserId * outsideTransaction:IDbTransaction -> Task
    abstract member DoHealthCheck : unit -> Task
    abstract member DeleteAggregates : entity:string * userId:UserId * outsideTransaction:IDbTransaction -> Task
    abstract member DeleteAggregate : entity:string * aggregateId:Guid * userId:UserId -> Task
