namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Stocks.PendingPositions
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/stocks/pendingpositions")>]
type PendingPositionsController(pendingStockPositionsHandler: PendingStockPositionsHandler) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.PendingStockPositions() =
        this.OkOrError(pendingStockPositionsHandler.Handle({Query.UserId = this.User.Identifier()}))

    [<HttpGet("export")>]
    member this.ExportPendingStockPositions() =
        this.GenerateExport(
            pendingStockPositionsHandler.Handle({Export.UserId = this.User.Identifier()})
        )

    [<HttpPost>]
    member this.CreatePendingStockPosition([<FromBody>] command: Create) =
        this.OkOrError(
            pendingStockPositionsHandler.HandleCreate (this.User.Identifier()) command
        )

    [<HttpPost("{id}/close")>]
    member this.ClosePendingStockPosition([<FromBody>] cmd: Close) =
        this.OkOrError(
            pendingStockPositionsHandler.Handle (cmd, this.User.Identifier())
        )
