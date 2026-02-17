namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Adapters.Stocks
open core.fs.Stocks
open core.Shared
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type StocksController(service: StocksHandler) =
    inherit ControllerBase()

    [<HttpGet("{ticker}")>]
    member this.Details([<FromRoute>] ticker: string) =
        this.OkOrError(
            service.Handle(
                {DetailsQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("{ticker}/prices")>]
    member this.Prices([<FromRoute>] ticker: string, [<FromQuery>] numberOfDays: int, [<FromQuery>] frequency: string) =
        this.OkOrError(
            service.Handle(
                PricesQuery.NumberOfDays(
                    frequency = PriceFrequency.FromString(frequency),
                    numberOfDays = numberOfDays,
                    ticker = Ticker(ticker),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpGet("{ticker}/secfilings")>]
    member this.SecFilings([<FromRoute>] ticker: string) =
        this.OkOrError(
            service.Handle(
                {CompanyFilingsQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("{ticker}/prices/{start}/{end}")>]
    member this.PricesRange(
        [<FromRoute>] ticker: string,
        [<FromRoute>] start: DateTimeOffset,
        [<FromRoute>] ``end``: DateTimeOffset,
        [<FromQuery>] frequency: string) =
        this.OkOrError(
            service.Handle(
                {
                    PricesQuery.Frequency = PriceFrequency.FromString(frequency)
                    UserId = this.User.Identifier()
                    Ticker = Ticker(ticker)
                    Start = Some start
                    End = Some ``end``
                }
            )
        )

    [<HttpGet("{ticker}/price")>]
    member this.Price([<FromRoute>] ticker: string) =
        this.OkOrError(
            service.Handle(
                {PriceQuery.UserId = this.User.Identifier(); Ticker = Ticker(ticker)}
            )
        )

    [<HttpGet("{ticker}/quote")>]
    member this.Quote([<FromRoute>] ticker: string) =
        this.OkOrError(
            service.Handle(
                {QuoteQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("search/{term}")>]
    member this.Search([<FromRoute>] term: string) =
        this.OkOrError(
            service.Handle(
                {SearchQuery.Term = term; UserId = this.User.Identifier()}
            )
        )
