namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Adapters.Stocks
open core.fs.Reports
open core.fs.Stocks
open core.Shared
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type ReportsController(service: ReportsHandler) =
    inherit ControllerBase()

    [<HttpGet("chain")>]
    member this.Chain() =
        service.Handle({ChainQuery.UserId = this.User.Identifier()})

    [<HttpGet("sells")>]
    member this.Sells() =
        this.OkOrError(service.Handle({SellsQuery.UserId = this.User.Identifier()}))

    [<HttpGet("pendingpositions")>]
    member this.PendingPositions() =
        this.OkOrError(service.Handle({PendingPositionsReportQuery.UserId = this.User.Identifier()}))

    [<HttpPost("outcomes")>]
    member this.TickersOutcomes([<FromBody>] query: OutcomesReportQuery) =
        this.OkOrError(service.HandleOutcomesReport (this.User.Identifier()) query)

    [<HttpGet("percentChangeDistribution/tickers/{ticker}")>]
    member this.TickerPercentChangeDistribution(ticker: string, [<FromQuery>] frequency: string) =
        let freq = frequency |> Option.ofObj |> Option.defaultValue (PriceFrequency.Daily.ToString())
        this.OkOrError(
            service.Handle(
                {
                    PercentChangeStatisticsQuery.Frequency = PriceFrequency.FromString(freq)
                    Ticker = Ticker(ticker)
                    UserId = this.User.Identifier()
                }
            )
        )

    [<HttpGet("gaps/tickers/{ticker}")>]
    member this.TickerGaps(ticker: string, [<FromQuery>] frequency: string) =
        this.OkOrError(
            service.Handle(
                {GapReportQuery.UserId = this.User.Identifier(); Ticker = Ticker(ticker); Frequency = PriceFrequency.FromString(frequency)}
            )
        )

    [<HttpGet("positions")>]
    member this.Portfolio() =
        this.OkOrError(service.Handle({OutcomesReportForPositionsQuery.UserId = this.User.Identifier()}))

    [<HttpGet("portfolio/correlations")>]
    member this.PortfolioCorrelations([<FromQuery>] days: int) =
        this.OkOrError(service.Handle({PortfolioCorrelationQuery.Days = days; UserId = this.User.Identifier()}))

    [<HttpPost("correlations")>]
    member this.Correlations([<FromBody>] query: CorrelationsQuery) =
        this.OkOrError(service.HandleCorrelationsQuery (this.User.Identifier()) query)

    [<HttpGet("portfolio/fundamentals")>]
    member this.PortfolioFundamentals() =
        this.OkOrError(service.Handle({PortfolioFundamentalsQuery.UserId = this.User.Identifier()}))

    [<HttpPost("fundamentals")>]
    member this.Fundamentals([<FromBody>] query: FundamentalsQuery) =
        this.OkOrError(service.HandleFundamentalsQuery (this.User.Identifier()) query)

    [<HttpGet("DailyPositionReport/{positionId}")>]
    member this.DailyPositionReport(positionId: string) =
        this.OkOrError(
            service.Handle(
                {
                    DailyPositionReportQuery.UserId = this.User.Identifier()
                    PositionId = StockPositionId(Guid.Parse(positionId))
                }
            )
        )

    [<HttpGet("DailyTickerReport/{ticker}")>]
    member this.DailyTickerReport(ticker: string, [<FromQuery>] startDate: string, [<FromQuery>] endDate: string) =
        this.OkOrError(
            service.Handle(
                {
                    DailyTickerReportQuery.UserId = this.User.Identifier()
                    Ticker = Ticker(ticker)
                    StartDate = Some startDate
                    EndDate = Some endDate
                }
            )
        )

    [<HttpGet("trends/{ticker}")>]
    member this.Trends(ticker: string, [<FromQuery>] start: string, [<FromQuery>] ``end``: string, [<FromQuery>] trendType: string) =
        this.OkOrError(
            service.Handle(
                {
                    TrendsQuery.UserId = this.User.Identifier()
                    Ticker = Ticker(ticker)
                    TrendType = core.fs.Services.Trends.TrendType.FromString(trendType)
                    Start = Some start
                    End = Some ``end``
                }
            )
        )

    [<HttpGet("inflectionpoints/{ticker}")>]
    member this.InflectionPoints(ticker: string, [<FromQuery>] start: string, [<FromQuery>] ``end``: string) =
        this.OkOrError(
            service.Handle(
                {
                    InflectionPointsQuery.UserId = this.User.Identifier()
                    Ticker = Ticker(ticker)
                    Start = Some start
                    End = Some ``end``
                }
            )
        )

    [<HttpGet("weeklysummary")>]
    member this.Review(period: string) =
        service.Handle({WeeklySummaryQuery.Period = period; UserId = this.User.Identifier()})

    [<HttpGet("transactions")>]
    member this.Transactions(ticker: string, groupBy: string, show: string, txType: string) =
        let tickerOption =
            if String.IsNullOrWhiteSpace(ticker) then None
            else Some(Ticker(ticker))
        this.OkOrError(
            service.Handle(
                {
                    TransactionsQuery.UserId = this.User.Identifier()
                    Show = show
                    GroupBy = groupBy
                    TxType = txType
                    Ticker = tickerOption
                }
            )
        )

    [<HttpGet("transactions/export")>]
    member this.TransactionsExport(ticker: string, startDate: string) =
        let tickerOption =
            if String.IsNullOrWhiteSpace(ticker) then None
            else Some(Ticker(ticker))
        let startDateOption =
            if String.IsNullOrWhiteSpace(startDate) then None
            else Some startDate
        this.GenerateExport(
            service.Handle(
                {
                    TransactionsExportQuery.UserId = this.User.Identifier()
                    Ticker = tickerOption
                    StartDate = startDateOption
                }
            )
        )
