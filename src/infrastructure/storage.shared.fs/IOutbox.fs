namespace storage.shared

open System.Collections.Generic
open System.Data
open System.Threading.Tasks
open core.fs
open core.Shared
open Microsoft.FSharp.Core

type IOutbox =
    abstract member AddEvents : e:List<AggregateEvent> * tx:IDbTransaction -> Task<Result<unit, ServiceError>>
