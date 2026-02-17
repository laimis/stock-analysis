namespace web.Utils

open System.Threading.Tasks
open core.fs
open core.fs.Adapters.CSV
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Primitives
open Microsoft.FSharp.Core

[<AutoOpen>]
module ControllerBaseExtensions =
    
    type ControllerBase with
        
        member this.GenerateExport(response: ExportResponse) =
            this.HttpContext.Response.Headers.ContentDisposition <- StringValues($"attachment; filename={response.Filename}")
            
            ContentResult(
                Content = response.Content,
                ContentType = response.ContentType
            ) :> ActionResult
        
        member this.GenerateExport(responseTask: Task<Result<ExportResponse, ServiceError>>) = task {
            let! response = responseTask
            
            match response with
            | Error error -> return this.Error(error)
            | Ok exportResponse -> return this.GenerateExport(exportResponse)
        }
        
        member this.Error(error: ServiceError) =
            let dict = Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()
            dict.AddModelError("error", error.Message)
            this.BadRequest(dict) :> ActionResult
        
        member this.OkOrError(r: Task<Result<unit, ServiceError>>) = task {
            let! result = r
            return this.OkOrError(result)
        }
        
        member this.OkOrError(r: Task<Result<'T, ServiceError>>) = task {
            let! result = r
            return this.OkOrError(result)
        }
        
        member this.OkOrError(r: Result<unit, ServiceError>) =
            match r with
            | Error error -> this.Error(error)
            | Ok () -> this.Ok() :> ActionResult
        
        member this.OkOrError(r: Result<'T, ServiceError>) =
            match r with
            | Error error -> this.Error(error)
            | Ok value -> this.Ok(value) :> ActionResult
