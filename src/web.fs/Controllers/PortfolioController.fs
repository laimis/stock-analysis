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
    member this.PortfolioHoldings() =
        this.OkOrError(stockPositionHandler.Handle(PortfolioHoldingsQuery(this.User.Identifier())))

    [<HttpGet("stockpositions/export/closed")>]
    member this.ExportClosed() =
        this.GenerateExport(
            stockPositionHandler.Handle(
                ExportTrades(this.User.Identifier(), ExportType.Closed)
            )
        )

    [<HttpGet("stockpositions/export/open")>]
    member this.ExportTrades() =
        this.GenerateExport(
            stockPositionHandler.Handle(
                ExportTrades(this.User.Identifier(), ExportType.Open)
            )
        )

    [<HttpGet("stockpositions/export/transactions")>]
    member this.Export() = task {
        let! result = stockPositionHandler.Handle(ExportTransactions(this.User.Identifier()))
        return this.GenerateExport(result)
    }

    [<HttpGet("stockpositions/simulate/trades")>]
    member this.Trade([<FromQuery>] closePositionIfOpenAtTheEnd: bool, [<FromQuery>] numberOfTrades: int) =
        this.OkOrError(
            stockPositionHandler.Handle(
                SimulateUserTrades(
                    closePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd,
                    numberOfTrades = numberOfTrades,
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpGet("stockpositions/simulate/opentrades/notices")>]
    member this.SimulateOpenTradesNotices() =
        this.OkOrError(
            stockPositionHandler.Handle(
                SimulateOpenTrades(userId = this.User.Identifier())
            )
        )

    [<HttpGet("stockpositions/simulate/trades/export")>]
    member this.SimulateTradesExport([<FromQuery>] closePositionIfOpenAtTheEnd: bool, [<FromQuery>] numberOfTrades: int) =
        this.GenerateExport(
            stockPositionHandler.HandleExport(
                ExportUserSimulatedTrades(
                    userId = this.User.Identifier(),
                    closePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd,
                    numberOfTrades = numberOfTrades
                )
            )
        )

    [<HttpGet("stockpositions/ownership/{ticker}")>]
    member this.Ownership([<FromRoute>] ticker: string) =
        this.OkOrError(
            stockPositionHandler.Handle(
                OwnershipQuery(Ticker(ticker), this.User.Identifier())
            )
        )

    [<HttpGet("{ticker}/simulate/trades")>]
    member this.TradeForTicker(
        [<FromRoute>] ticker: string,
        [<FromQuery>] numberOfShares: decimal,
        [<FromQuery>] price: decimal,
        [<FromQuery>] stopPrice: decimal,
        [<FromQuery>] ``when``: string) =
        this.OkOrError(
            stockPositionHandler.Handle(
                SimulateTradeForTicker(
                    userId = this.User.Identifier(),
                    ticker = Ticker(ticker),
                    numberOfShares = numberOfShares,
                    price = price,
                    stopPrice = stopPrice,
                    date = DateTimeOffset.Parse(``when``)
                )
            )
        )

    [<HttpGet("stockpositions/tradingentries")>]
    member this.TradingEntries() =
        this.OkOrError(
            stockPositionHandler.Handle(QueryTradingEntries(this.User.Identifier()))
        )

    [<HttpGet("stockpositions/pasttradingentries")>]
    member this.PastTradingEntries() =
        this.OkOrError(
            stockPositionHandler.Handle(QueryPastTradingEntries(this.User.Identifier()))
        )

    [<HttpGet("stockpositions/pasttradingperformance")>]
    member this.PastTradingPerformance() =
        this.OkOrError(
            stockPositionHandler.Handle(QueryPastTradingPerformance(this.User.Identifier()))
        )

    [<HttpPost("stockpositions/import")>]
    member this.Import(file: IFormFile) = task {
        use streamReader = new StreamReader(file.OpenReadStream())
        let! content = streamReader.ReadToEndAsync()
        let cmd = ImportStocks(userId = this.User.Identifier(), content = content)
        return! this.OkOrError(stockPositionHandler.Handle(cmd))
    }

    [<HttpPost("stockpositions/{positionId}/sell")>]
    member this.Sell([<FromBody>] model: StockTransaction) =
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.NewSell(model, this.User.Identifier())))

    [<HttpPost("stockpositions/{positionId}/buy")>]
    member this.Buy([<FromBody>] model: StockTransaction) =
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.NewBuy(model, this.User.Identifier())))

    [<HttpPost("stockpositions/{positionId}/notes")>]
    member this.AddNotes([<FromBody>] model: AddNotes) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), model))

    [<HttpGet("stockpositions/{positionId}/simulate/trades")>]
    member this.TradeForPosition([<FromRoute>] positionId: string, [<FromQuery>] closeIfOpenAtTheEnd: bool) =
        this.OkOrError(
            stockPositionHandler.Handle(
                SimulateTrade(
                    closeIfOpenAtTheEnd = closeIfOpenAtTheEnd,
                    positionId = StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpGet("stockpositions/{positionId}/profitpoints")>]
    member this.ProfitPoints([<FromRoute>] positionId: string, [<FromQuery>] numberOfPoints: int) =
        this.OkOrError(
            stockPositionHandler.Handle(
                ProfitPointsQuery(
                    numberOfPoints = numberOfPoints,
                    positionId = StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpPost("stockpositions/{positionId}/grade")>]
    member this.Grade([<FromBody>] command: GradePosition) =
        this.OkOrError(stockPositionHandler.HandleGradePosition(this.User.Identifier(), command))

    [<HttpPost("stockpositions")>]
    member this.OpenPosition([<FromBody>] command: OpenStockPosition) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}")>]
    member this.DeleteStockPosition([<FromRoute>] positionId: string) =
        this.OkOrError(
            stockPositionHandler.Handle(
                DeletePosition(
                    positionId = StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpGet("stockpositions/{positionId}")>]
    member this.Position([<FromRoute>] positionId: string) =
        this.OkOrError(
            stockPositionHandler.Handle(
                QueryPosition(
                    positionId = StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpPost("stockpositions/{positionId}/issueclosingorders")>]
    member this.IssuePositionClosingOrders([<FromBody>] command: IssuePositionClosingOrders) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("stockpositions/{positionId}/labels")>]
    member this.SetLabel([<FromBody>] command: AddLabel) =
        this.OkOrError(stockPositionHandler.HandleAddLabel(this.User.Identifier(), command))

    [<HttpPost("stockpositions/{positionId}/stop")>]
    member this.Stop([<FromBody>] command: SetStop) =
        this.OkOrError(stockPositionHandler.HandleStop(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}/labels/{label}")>]
    member this.RemoveLabel([<FromRoute>] positionId: string, [<FromRoute>] label: string) =
        this.OkOrError(
            stockPositionHandler.Handle(
                RemoveLabel(
                    positionId = StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    key = label,
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpPost("stockpositions/{positionId}/risk")>]
    member this.Risk(command: SetRisk) =
        this.OkOrError(stockPositionHandler.HandleSetRisk(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}/transactions/{eventId}")>]
    member this.DeleteTransaction([<FromRoute>] positionId: string, [<FromRoute>] eventId: Guid) =
        this.OkOrError(
            stockPositionHandler.Handle(
                DeleteTransaction(
                    StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    this.User.Identifier(),
                    eventId
                )
            )
        )

    [<HttpPost("stockpositions/{positionId}/reinvestdividend")>]
    member this.ReinvestDividend([<FromBody>] command: ReinvestDividendCommand) =
        this.OkOrError(stockPositionHandler.Handle(this.User.Identifier(), command))

    [<HttpDelete("stockpositions/{positionId}/stop")>]
    member this.DeleteStop([<FromRoute>] positionId: string) = task {
        let! result = stockPositionHandler.Handle(this.User.Identifier(), DeleteStop(StockPositionId.NewStockPositionId(Guid.Parse(positionId))))
        return this.OkOrError(result)
    }

    [<HttpPost("optionpositions")>]
    member this.OpenOptionPosition([<FromBody>] command: OpenOptionPositionCommand) =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("optionpositions/pending")>]
    member this.OpenPendingOptionPosition([<FromBody>] command: CreatePendingOptionPositionCommand) =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpGet("optionpositions/ownership/{ticker}")>]
    member this.OptionOwnership([<FromRoute>] ticker: string) =
        this.OkOrError(
            optionsHandler.Handle(
                OptionOwnershipQuery(ticker = Ticker(ticker), userId = this.User.Identifier())
            )
        )

    [<HttpGet("optionpositions/{optionId:guid}")>]
    member this.Get([<FromRoute>] optionId: Guid) =
        this.OkOrError(
            optionsHandler.Handle(
                OptionPositionQuery(
                    positionId = OptionPositionId.NewOptionPositionId(optionId),
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpDelete("optionpositions/{id:guid}")>]
    member this.DeleteOptionPosition([<FromRoute>] id: Guid) =
        this.OkOrError(
            optionsHandler.Handle(
                DeleteOptionPositionCommand(
                    OptionPositionId.NewOptionPositionId(id),
                    this.User.Identifier()
                )
            )
        )

    [<HttpGet("options")>]
    member this.OptionsDashboar() =
        this.OkOrError(optionsHandler.Handle(DashboardQuery(this.User.Identifier())))

    [<HttpDelete("optionpositions/{positionId}/labels/{label}")>]
    member this.RemoveOptionLabel([<FromRoute>] positionId: string, [<FromRoute>] label: string) =
        this.OkOrError(
            optionsHandler.Handle(
                RemoveOptionPositionLabelCommand(
                    positionId = OptionPositionId.NewOptionPositionId(Guid.Parse(positionId)),
                    key = label,
                    userId = this.User.Identifier()
                )
            )
        )

    [<HttpPost("optionpositions/{positionId}/labels")>]
    member this.SetOptionLabel([<FromBody>] command: SetOptionPositionLabel) =
        this.OkOrError(optionsHandler.Handle(this.User.Identifier(), command))

    [<HttpPost("optionpositions/{positionId}/closecontracts")>]
    member this.CloseContracts([<FromRoute>] positionId: Guid, [<FromBody>] contracts: OptionContractInput[]) =
        this.OkOrError(
            optionsHandler.Handle(
                CloseContractsCommand(
                    positionId = OptionPositionId.NewOptionPositionId(positionId),
                    userId = this.User.Identifier(),
                    contracts = contracts
                )
            )
        )

    [<HttpPost("optionpositions/{positionId}/opencontracts")>]
    member this.OpenContracts([<FromRoute>] positionId: Guid, [<FromBody>] contracts: OptionContractInput[]) =
        this.OkOrError(
            optionsHandler.Handle(
                OpenContractsCommand(
                    positionId = OptionPositionId.NewOptionPositionId(positionId),
                    userId = this.User.Identifier(),
                    contracts = contracts
                )
            )
        )

    [<HttpPost("optionpositions/{positionId}/notes")>]
    member this.AddOptionNotes([<FromRoute>] positionId: Guid, [<FromBody>] content: string) =
        this.OkOrError(
            optionsHandler.Handle(
                AddOptionNotesCommand(
                    positionId = OptionPositionId.NewOptionPositionId(positionId),
                    userId = this.User.Identifier(),
                    notes = content
                )
            )
        )

    [<HttpPost("optionpositions/{positionId}/close")>]
    member this.CloseOptionPosition([<FromBody>] command: CloseOptionPositionCommand) =
        this.OkOrError(optionsHandler.Handle(command, this.User.Identifier()))
