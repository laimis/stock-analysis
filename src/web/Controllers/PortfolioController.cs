using System;
using System.IO;
using System.Threading.Tasks;
using core.fs.Options;
using core.fs.Portfolio;
using core.fs.Stocks;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;
using web.Utils;
using OwnershipQuery = core.fs.Portfolio.OwnershipQuery;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PortfolioController(
    StockPositionHandler stockPositionHandler,
    OptionsHandler optionsHandler) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> Index() =>
        this.OkOrError(stockPositionHandler.Handle(new Query(User.Identifier())));
    
    [HttpGet("transactions")]
    public Task<ActionResult> Transactions(string ticker, string groupBy, string show, string txType) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new QueryTransactions(
                    userId: User.Identifier(),
                    show: show,
                    groupBy: groupBy,
                    txType: txType,
                    ticker: string.IsNullOrWhiteSpace(ticker) ? FSharpOption<Ticker>.None : new FSharpOption<Ticker>(new Ticker(ticker)))
            )
        );

    [HttpGet("transactionsummary")]
    public Task<TransactionSummaryView> Review(string period) =>
        stockPositionHandler.Handle(
            new TransactionSummary(period: period, userId: User.Identifier())
        );

    [HttpGet("stockpositions/export/closed")]
    public Task<ActionResult> ExportClosed() =>
        this.GenerateExport(
            stockPositionHandler.Handle(
                new ExportTrades(User.Identifier(), ExportType.Closed)
            )
        );

    [HttpGet("stockpositions/export/open")]
    public Task<ActionResult> ExportTrades() =>
        this.GenerateExport(
            stockPositionHandler.Handle(
                new ExportTrades(User.Identifier(), ExportType.Open)
            )
        );
    
    [HttpGet("stockpositions/export/transactions")]
    public async Task<ActionResult> Export() =>
        this.GenerateExport(
            await stockPositionHandler.Handle(
                new ExportTransactions(User.Identifier())
            )
        );
    
    [HttpGet("stockpositions/simulate/trades")]
    public Task<ActionResult> Trade(
        [FromQuery] bool closePositionIfOpenAtTheEnd,
        [FromQuery] bool adjustSizeBasedOnRisk,
        [FromQuery] int numberOfTrades) =>

        this.OkOrError(
            stockPositionHandler.Handle(
                new SimulateUserTrades(
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades,
                    adjustSizeBasedOnRisk: adjustSizeBasedOnRisk,
                    userId: User.Identifier()
                )
            )
        );
    
    [HttpGet("stockpositions/simulate/opentrades/notices")]
    public Task<ActionResult> SimulateOpenTradesNotices() =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new SimulateOpenTrades(
                    userId: User.Identifier()
                )
            )
        );

    [HttpGet("stockpositions/simulate/trades/export")]
    public Task<ActionResult> SimulateTradesExport(
        [FromQuery] bool closePositionIfOpenAtTheEnd,
        [FromQuery] bool adjustSizeBasedOnRisk,
        [FromQuery] int numberOfTrades) =>

        this.GenerateExport(
            stockPositionHandler.HandleExport(
                new ExportUserSimulatedTrades(
                    userId: User.Identifier(),
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    adjustSizeBasedOnRisk: adjustSizeBasedOnRisk,
                    numberOfTrades: numberOfTrades
                )
            )
        );

    [HttpGet("stockpositions/ownership/{ticker}")]
    public Task<ActionResult> Ownership([FromRoute] string ticker) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new OwnershipQuery(
                    new Ticker(ticker), User.Identifier()
                )
            )
        );
    
    
    [HttpGet("{ticker}/simulate/trades")]
    public Task<ActionResult> Trade(
        [FromRoute] string ticker,
        [FromQuery] decimal numberOfShares,
        [FromQuery] decimal price,
        [FromQuery] decimal stopPrice,
        [FromQuery] string when) =>

        this.OkOrError(
            stockPositionHandler.Handle(
                new SimulateTradeForTicker(
                    userId: User.Identifier(),
                    ticker: new Ticker(ticker),
                    numberOfShares: numberOfShares,
                    price: price,
                    stopPrice: stopPrice,
                    date: DateTimeOffset.Parse(when))
            )
        );

    [HttpGet("stockpositions/tradingentries")]
    public Task<ActionResult> TradingEntries() =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new QueryTradingEntries(
                    User.Identifier()
                )
            )
        );
        
    [HttpGet("stockpositions/pasttradingentries")]
    public Task<ActionResult> PastTradingEntries() =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new QueryPastTradingEntries(
                    User.Identifier()
                )
            )
        );
    
    [HttpGet("stockpositions/pasttradingperformance")]
    public Task<ActionResult> PastTradingPerformance() =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new QueryPastTradingPerformance(
                    User.Identifier()
                )
            )
        );
    
    
    [HttpPost("stockpositions/import")]
    public async Task<ActionResult> Import(IFormFile file)
    {
        using var streamReader = new StreamReader(file.OpenReadStream());

        var content = await streamReader.ReadToEndAsync();

        var cmd = new ImportStocks(userId: User.Identifier(), content: content);
            
        return await this.OkOrError(stockPositionHandler.Handle(cmd));
    }
    
    [HttpPost("stockpositions/{positionId}/sell")]
    public Task<ActionResult> Sell([FromBody]StockTransaction model) =>
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.NewSell(model, User.Identifier())));

    [HttpPost("stockpositions/{positionId}/buy")]
    public Task<ActionResult> Buy([FromBody]StockTransaction model) =>
        this.OkOrError(stockPositionHandler.Handle(BuyOrSell.NewBuy(model, User.Identifier())));
    
    [HttpPost("stockpositions/{positionId}/notes")]
    public Task<ActionResult> AddNotes([FromBody]AddNotes model) =>
        this.OkOrError(stockPositionHandler.Handle(User.Identifier(), model));

    [HttpGet("stockpositions/{positionId}/simulate/trades")]
    public Task<ActionResult> Trade([FromRoute] string positionId, [FromQuery]bool closeIfOpenAtTheEnd) =>

        this.OkOrError(
            stockPositionHandler.Handle(
                new SimulateTrade(
                    closeIfOpenAtTheEnd: closeIfOpenAtTheEnd,
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId: User.Identifier()
                )
            )
        );

    [HttpGet("stockpositions/{positionId}/profitpoints")]
    public Task<ActionResult> ProfitPoints(
        [FromRoute] string positionId,
        [FromQuery] int numberOfPoints) =>

        this.OkOrError(
            stockPositionHandler.Handle(
                new ProfitPointsQuery(
                    numberOfPoints: numberOfPoints,
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("stockpositions/{positionId}/grade")]
    public Task Grade([FromBody]GradePosition command) =>
        this.OkOrError(
            stockPositionHandler.HandleGradePosition(
                User.Identifier(), command
            )
        );
    
    [HttpPost("stockpositions")]
    public Task<ActionResult> OpenPosition([FromBody]OpenStockPosition command) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                User.Identifier(), command
            )
        );

    [HttpDelete("stockpositions/{positionId}")]
    public Task<ActionResult> DeleteStockPosition(
        [FromRoute] string positionId) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new DeletePosition(
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId: User.Identifier()
                )
            )
        );
    
    [HttpGet("stockpositions/{positionId}")]
    public Task<ActionResult> Position([FromRoute] string positionId) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new QueryPosition(
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId: User.Identifier()
                )
            )
        );
    
    [HttpPost("stockpositions/{positionId}/close")]
    public Task<ActionResult> ClosePosition([FromBody] ClosePosition command) =>
        this.OkOrError(stockPositionHandler.Handle(User.Identifier(), command));

    [HttpPost("stockpositions/{positionId}/labels")]
    public Task SetLabel([FromBody]AddLabel command) =>
        this.OkOrError(
            stockPositionHandler.HandleAddLabel(
                User.Identifier(), command
            )
        );
    
    [HttpPost("stockpositions/{positionId}/stop")]
    public Task<ActionResult> Stop([FromBody] SetStop command) =>
        this.OkOrError(stockPositionHandler.HandleStop(User.Identifier(), command));

    [HttpDelete("stockpositions/{positionId}/labels/{label}")]
    public Task RemoveLabel(
        [FromRoute] string positionId,
        [FromRoute] string label) => 
        this.OkOrError(
            stockPositionHandler.Handle(
                new RemoveLabel(
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    key: label,
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("stockpositions/{positionId}/risk")]
    public Task<ActionResult> Risk(SetRisk command) =>
        this.OkOrError(
            stockPositionHandler.HandleSetRisk(
                User.Identifier(), command
            )
        );
    
    [HttpDelete("stockpositions/{positionId}/transactions/{eventId}")]
    public Task<ActionResult> DeleteTransaction([FromRoute] string positionId, [FromRoute] Guid eventId) =>
        this.OkOrError(
            stockPositionHandler.Handle(
                new DeleteTransaction(StockPositionId.NewStockPositionId(Guid.Parse(positionId)), User.Identifier(), eventId
                )
            )
        );
    
    [HttpDelete("stockpositions/{positionId}/stop")]
    public async Task<ActionResult> DeleteStop([FromRoute] string positionId) =>
        this.OkOrError(await stockPositionHandler.Handle(User.Identifier(), new DeleteStop(StockPositionId.NewStockPositionId(Guid.Parse(positionId)))));
    
    [HttpPost("optionpositions")]
    public Task<ActionResult> OpenOptionPosition([FromBody]OpenOptionPositionCommand command) =>
        this.OkOrError(
            optionsHandler.Handle(
                User.Identifier(), command
            )
        );
    
    [HttpGet("optionpositions/ownership/{ticker}")]
    public Task<ActionResult> OptionOwnership([FromRoute] string ticker) =>
        this.OkOrError(
            optionsHandler.Handle(
                new OptionOwnershipQuery(
                    ticker: new Ticker(ticker), userId: User.Identifier()
                )
            )
        );
        
    [HttpGet("optionpositions/{optionId:guid}")]
    public Task<ActionResult> Get([FromRoute] Guid optionId) =>
        this.OkOrError(
            optionsHandler.Handle(
                new OptionPositionQuery(positionId: OptionPositionId.NewOptionPositionId(optionId), userId: User.Identifier()
                )
            )
        );
    
    [HttpDelete("optionpositions/{id:guid}")]
    public Task<ActionResult> DeleteOptionPosition([FromRoute] Guid id)
        => this.OkOrError(
            optionsHandler.Handle(
                new DeleteOptionPositionCommand(OptionPositionId.NewOptionPositionId(id), User.Identifier())
            )
        );

    [HttpGet("options")]
    public Task<ActionResult> OptionsDashboar()
        => this.OkOrError(
            optionsHandler.Handle(
                new DashboardQuery(
                    User.Identifier()
                )
            ));
}
