namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Stocks.Lists
open core.Shared
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/stocks/lists")>]
type StockListsController(handler: Handler) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.StockLists() =
        this.OkOrError(handler.Handle({GetLists.UserId = this.User.Identifier()}))

    [<HttpPost>]
    member this.CreateStockList([<FromBody>] command: Create) =
        this.OkOrError(handler.HandleCreate (this.User.Identifier()) command)

    [<HttpDelete("{id}")>]
    member this.DeleteStockList([<FromRoute>] id: Guid) =
        this.OkOrError(handler.Handle({Delete.Id = id; UserId = this.User.Identifier()}))

    [<HttpPost("{id}")>]
    member this.UpdateStockList([<FromBody>] command: Update) =
        this.OkOrError(handler.HandleUpdate command (this.User.Identifier()))

    [<HttpPut("{id}")>]
    member this.AddStockToList([<FromBody>] command: AddStockToList, [<FromServices>] service: Handler) =
        this.OkOrError(handler.HandleAddStockToList (this.User.Identifier()) command)

    [<HttpDelete("{id}/{ticker}")>]
    member this.RemoveStockFromList([<FromRoute>] id: Guid, [<FromRoute>] ticker: string, [<FromServices>] service: Handler) =
        this.OkOrError(handler.Handle({RemoveStockFromList.Id = id; UserId = this.User.Identifier(); Ticker = Ticker(ticker)}))

    [<HttpPut("{id}/tags")>]
    member this.AddTagToStockList([<FromBody>] command: AddTagToList) =
        this.OkOrError(handler.HandleAddTagToList (this.User.Identifier()) command)

    [<HttpDelete("{id}/tags/{tag}")>]
    member this.RemoveTagFromStockList([<FromRoute>] id: Guid, [<FromRoute>] tag: string) =
        this.OkOrError(handler.Handle({RemoveTagFromList.Tag = tag; Id = id; UserId = this.User.Identifier()}))

    [<HttpGet("{id}")>]
    member this.GetStockList([<FromRoute>] id: Guid) =
        this.OkOrError(handler.Handle({GetList.Id = id; UserId = this.User.Identifier()}))

    [<HttpGet("{id}/export")>]
    member this.ExportStockList(id: Guid, [<FromQuery>] justTickers: bool) =
        this.GenerateExport(
            handler.Handle(
                {ExportList.JustTickers = justTickers; Id = id; UserId = this.User.Identifier()}
            )
        )

    [<HttpPost("{id}/clear")>]
    member this.ClearStockList([<FromRoute>] id: Guid) =
        this.OkOrError(handler.Handle({Clear.Id = id; UserId = this.User.Identifier()}))
