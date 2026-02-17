namespace core.Shared

open System.Threading.Tasks
open Microsoft.FSharp.Core

type IBlobStorage =
    abstract member Get<'T> : string -> Task<'T option>
    abstract member Save<'T> : string -> 'T -> Task
