namespace web.Controllers

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open core.fs.Options
open core.fs.Portfolio
open core.fs.Stocks
open core.Shared
open web.Utils

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type PortfolioController(stockPositionHandler: StockPositionHandler, optionsHandler: OptionsHandler) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.PortfolioHoldings() : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.Handle({PortfolioHoldingsQuery.UserId = this.User.Identifier()}))

    [<HttpGet("stockpositions/export/closed")>]
    member this.ExportClosed() : Task<ActionResult> =
        this.GenerateExport(
            stockPositionHandler.Handle(
                {ExportTrades.UserId = this.User.Identifier(); ExportType = ExportType.Closed}
            )
        )

    [<HttpGet("stockpositions/export/open")>]
    member this.ExportTrades() : Task<ActionResult> =
        this.GenerateExport(
            stockPositionHandler.Handle(
                {ExportTrades.UserId = this.User.Identifier(); ExportType = ExportType.Open}
            )
        )

    [<HttpGet("stockpositions/export/transactions")>]
    member this.Export() : Task<ActionResult> = task {
        let! result = stockPositionHandler.Handle({ExportTransactions.UserId = this.User.Identifier()})
        return this.GenerateExport(result)
    }

    [<HttpGet("stockpositions/simulate/trades")>]
    member this.Trade([<FromQuery>] closePositionIfOpenAtTheEnd: bool, [<FromQuery>] numberOfTrades: int) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    SimulateUserTrades.UserId = this.User.Identifier()
                    NumberOfTrades = numberOfTrades
                    ClosePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd
                }
            )
        )

    [<HttpGet("stockpositions/simulate/opentrades/notices")>]
    member this.SimulateOpenTradesNotices() : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {SimulateOpenTrades.UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("stockpositions/simulate/trades/export")>]
    member this.SimulateTradesExport([<FromQuery>] closePositionIfOpenAtTheEnd: bool, [<FromQuery>] numberOfTrades: int) : Task<ActionResult> =
        this.GenerateExport(
            stockPositionHandler.HandleExport(
                {
                    ExportUserSimulatedTrades.UserId = this.User.Identifier()
                    NumberOfTrades = numberOfTrades
                    ClosePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd
                }
            )
        )

    [<HttpGet("stockpositions/ownership/{ticker}")>]
    member this.Ownership([<FromRoute>] ticker: string) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {OwnershipQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("{ticker}/simulate/trades")>]
    member this.TradeForTicker(
        [<FromRoute>] ticker: string,
        [<FromQuery>] numberOfShares: decimal,
        [<FromQuery>] price: decimal,
        [<FromQuery>] stopPrice: decimal,
        [<FromQuery>] ``when``: string) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    SimulateTradeForTicker.UserId = this.User.Identifier()
                    Ticker = Ticker(ticker)
                    NumberOfShares = numberOfShares
                    Price = price
                    StopPrice = Some stopPrice
                    Date = DateTimeOffset.Parse(``when``)
                }
            )
        )

    [<HttpGet("stockpositions/tradingentries")>]
    member this.TradingEntries() : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle({QueryTradingEntries.UserId = this.User.Identifier()})
        )

    [<HttpGet("stockpositions/pasttradingentries")>]
    member this.PastTradingEntries() : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle({QueryPastTradingEntries.UserId = this.User.Identifier()})
        )

    [<HttpGet("stockpositions/pasttradingperformance")>]
    member this.PastTradingPerformance() : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle({QueryPastTradingPerformance.UserId = this.User.Identifier()})
        )

    [<HttpPost("stockpositions/import")>]
    member this.Import(file: IFormFile) : Task<ActionResult> = task {
        use streamReader = new StreamReader(file.OpenReadStream())
        let! content = streamReader.ReadToEndAsync()
        let cmd = {ImportStocks.UserId = this.User.Identifier(); Content = content}
        return! this.OkOrError(stockPositionHandler.Handle(cmd))
    }

    [<HttpPost("stockpositions/{positionId}/sell")>]
    member this.Sell([<FromBody>] model: StockTransaction) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.Sell(model, this.User.Identifier())))

    [<HttpPost("stockpositions/{positionId}/buy")>]
    member this.Buy([<FromBody>] model: StockTransaction) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.Buy(model, this.User.Identifier())))

    [<HttpPost("stockpositions/{positionId}/notes")>]
    member this.AddNotes([<FromBody>] model: AddNotes) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), model))

    [<HttpGet("stockpositions/{positionId}/simulate/trades")>]
    member this.TradeForPosition([<FromRoute>] positionId: string, [<FromQuery>] closeIfOpenAtTheEnd: bool) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    SimulateTrade.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                    CloseIfOpenAtTheEnd = closeIfOpenAtTheEnd
                }
            )
        )

    [<HttpGet("stockpositions/{positionId}/profitpoints")>]
    member this.ProfitPoints([<FromRoute>] positionId: string, [<FromQuery>] numberOfPoints: int) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    ProfitPointsQuery.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                    NumberOfPoints = numberOfPoints
                }
            )
        )

    [<HttpPost("stockpositions/{positionId}/grade")>]
    member this.Grade([<FromBody>] command: GradePosition) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.HandleGradePosition (this.User.Identifier()) command)

    [<HttpPost("stockpositions")>]
    member this.OpenPosition([<FromBody>] command: OpenStockPosition) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}")>]
    member this.DeleteStockPosition([<FromRoute>] positionId: string) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    DeletePosition.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                }
            )
        )

    [<HttpGet("stockpositions/{positionId}")>]
    member this.Position([<FromRoute>] positionId: string) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    QueryPosition.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                }
            )
        )

    [<HttpPost("stockpositions/{positionId}/issueclosingorders")>]
    member this.IssuePositionClosingOrders([<FromBody>] command: IssuePositionClosingOrders) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("stockpositions/{positionId}/labels")>]
    member this.SetLabel([<FromBody>] command: AddLabel) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.HandleAddLabel (this.User.Identifier()) command)

    [<HttpPost("stockpositions/{positionId}/stop")>]
    member this.Stop([<FromBody>] command: SetStop) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.HandleStop(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}/labels/{label}")>]
    member this.RemoveLabel([<FromRoute>] positionId: string, [<FromRoute>] label: string) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    RemoveLabel.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                    Key = label
                }
            )
        )

    [<HttpPost("stockpositions/{positionId}/risk")>]
    member this.Risk(command: SetRisk) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.HandleSetRisk (this.User.Identifier()) command)

    [<HttpDelete("stockpositions/{positionId}/transactions/{eventId}")>]
    member this.DeleteTransaction([<FromRoute>] positionId: string, [<FromRoute>] eventId: Guid) : Task<ActionResult> =
        this.OkOrError(
            stockPositionHandler.Handle(
                {
                    DeleteTransaction.PositionId = StockPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                    TransactionId = eventId
                }
            )
        )

    [<HttpPost("stockpositions/{positionId}/reinvestdividend")>]
    member this.ReinvestDividend([<FromBody>] command: ReinvestDividendCommand) : Task<ActionResult> =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}/stop")>]
    member this.DeleteStop([<FromRoute>] positionId: string) : Task<ActionResult> = task {
        let! result = stockPositionHandler.Handle(this.User.Identifier(), {DeleteStop.PositionId = StockPositionId(Guid.Parse(positionId))})
        return this.OkOrError(result)
    }

    [<HttpPost("optionpositions")>]
    member this.OpenOptionPosition([<FromBody>] command: OpenOptionPositionCommand) : Task<ActionResult> =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("optionpositions/pending")>]
    member this.OpenPendingOptionPosition([<FromBody>] command: CreatePendingOptionPositionCommand) : Task<ActionResult> =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpGet("optionpositions/ownership/{ticker}")>]
    member this.OptionOwnership([<FromRoute>] ticker: string) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {OptionOwnershipQuery.Ticker = Ticker(ticker); UserId = this.User.Identifier()}
            )
        )

    [<HttpGet("optionpositions/{optionId:guid}")>]
    member this.Get([<FromRoute>] optionId: Guid) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    OptionPositionQuery.PositionId = OptionPositionId(optionId)
                    UserId = this.User.Identifier()
                }
            )
        )

    [<HttpDelete("optionpositions/{id:guid}")>]
    member this.DeleteOptionPosition([<FromRoute>] id: Guid) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    DeleteOptionPositionCommand.PositionId = OptionPositionId(id)
                    UserId = this.User.Identifier()
                }
            )
        )

    [<HttpGet("options")>]
    member this.OptionsDashboar() : Task<ActionResult> =
        this.OkOrError(optionsHandler.Handle({DashboardQuery.UserId = this.User.Identifier()}))

    [<HttpDelete("optionpositions/{positionId}/labels/{label}")>]
    member this.RemoveOptionLabel([<FromRoute>] positionId: string, [<FromRoute>] label: string) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    RemoveOptionPositionLabelCommand.PositionId = OptionPositionId(Guid.Parse(positionId))
                    UserId = this.User.Identifier()
                    Key = label
                }
            )
        )

    [<HttpPost("optionpositions/{positionId}/labels")>]
    member this.SetOptionLabel([<FromBody>] command: SetOptionPositionLabel) : Task<ActionResult> =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("optionpositions/{positionId}/closecontracts")>]
    member this.CloseContracts([<FromRoute>] positionId: Guid, [<FromBody>] contracts: OptionContractInput[]) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    CloseContractsCommand.PositionId = OptionPositionId(positionId)
                    UserId = this.User.Identifier()
                    Contracts = contracts
                }
            )
        )

    [<HttpPost("optionpositions/{positionId}/opencontracts")>]
    member this.OpenContracts([<FromRoute>] positionId: Guid, [<FromBody>] contracts: OptionContractInput[]) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    OpenContractsCommand.PositionId = OptionPositionId(positionId)
                    UserId = this.User.Identifier()
                    Contracts = contracts
                }
            )
        )

    [<HttpPost("optionpositions/{positionId}/notes")>]
    member this.AddOptionNotes([<FromRoute>] positionId: Guid, [<FromBody>] content: string) : Task<ActionResult> =
        this.OkOrError(
            optionsHandler.Handle(
                {
                    AddOptionNotesCommand.PositionId = OptionPositionId(positionId)
                    UserId = this.User.Identifier()
                    Notes = content
                }
            )
        )

    [<HttpPost("optionpositions/{positionId}/close")>]
    member this.CloseOptionPosition([<FromBody>] command: CloseOptionPositionCommand) : Task<ActionResult> =
        this.OkOrError(optionsHandler.Handle(command, this.User.Identifier()))
