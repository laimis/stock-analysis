namespace web.Controllers

open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Options
open core.Shared
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type OptionsController(handler: OptionsHandler) =
    inherit ControllerBase()

    [<HttpGet("pricing")>]
    member this.GetOptionPricing([<FromQuery>] symbol: string) =
        this.OkOrError(
            handler.Handle
                {
                    OptionPricingQuery.UserId = this.User.Identifier()
                    Symbol = OptionTicker.create(symbol)
                }
        )

    [<HttpGet("chain/{ticker}")>]
    member this.Chain([<FromRoute>] ticker: string) =
        this.OkOrError(
            handler.Handle
                {ChainQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
        )

    [<HttpGet("export")>]
    member this.Export() = task {
        let! result = handler.Handle {ExportQuery.UserId = this.User.Identifier()}
        return this.GenerateExport result
    }
