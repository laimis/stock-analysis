namespace web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Portfolio
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type SECController(handler: SECHandler) =
    inherit ControllerBase()

    [<HttpGet("search")>]
    member this.Search([<FromQuery>] query: string) =
        this.OkOrError(handler.Handle({SearchCompanies.Query = query}))

    [<HttpGet("filings/{ticker}")>]
    member this.GetFilings([<FromRoute>] ticker: string) =
        this.OkOrError(handler.Handle({GetFilingsForTicker.Ticker = ticker}))

    [<HttpGet("portfolio-filings")>]
    member this.GetPortfolioFilings() =
        this.OkOrError(handler.Handle({GetPortfolioFilings.UserId = this.User.Identifier()}))
