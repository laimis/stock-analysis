using System;
using System.IO;
using System.Threading.Tasks;
using core.fs.Portfolio;
using core.fs.Shared.Domain;
using core.fs.Stocks;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using Handler = core.fs.Portfolio.Handler;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly Handler _handler;
    public PortfolioController(Handler handler) => _handler = handler;
    
    [HttpGet]
    public Task<ActionResult> Index() =>
        this.OkOrError(_handler.Handle(new Query(User.Identifier())));
    
    [HttpPost("stockpositions/{positionId}/sell")]
    public Task<ActionResult> Sell([FromBody]StockTransaction model) =>
        this.OkOrError(_handler.Handle(BuyOrSell.NewSell(model, User.Identifier())));

    [HttpPost("buy")]
    public Task<ActionResult> Buy([FromBody]StockTransaction model) =>
        this.OkOrError(_handler.Handle(BuyOrSell.NewBuy(model, User.Identifier())));
    
    [HttpGet("transactions")]
    public Task<ActionResult> Transactions(string ticker, string groupBy, string show, string txType) =>
        this.OkOrError(
            _handler.Handle(
                new QueryTransactions(
                    userId: User.Identifier(),
                    show: show,
                    groupBy: groupBy,
                    txType: txType,
                    ticker: new Ticker(ticker))
            )
        );

    [HttpGet("transactionsummary")]
    public Task<ActionResult> Review(string period) =>
        this.OkOrError(_handler.Handle(new TransactionSummary(period: period, userId: User.Identifier())));

    [HttpGet("simulate/trades")]
    public Task<ActionResult> Trade(
        [FromQuery] bool closePositionIfOpenAtTheEnd,
        [FromQuery] int numberOfTrades) =>

        this.OkOrError(
            _handler.Handle(
                new SimulateUserTrades(
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades,
                    userId: User.Identifier()
                )
            )
        );

    [HttpGet("simulate/trades/export")]
    public Task<ActionResult> SimulateTradesExport(
        [FromQuery] bool closePositionIfOpenAtTheEnd,
        [FromQuery] int numberOfTrades) =>

        this.GenerateExport(
            _handler.HandleExport(
                new ExportUserSimulatedTrades(
                    userId: User.Identifier(),
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades
                )
            )
        );

    [HttpGet("{ticker}/positions/{positionId}/simulate/trades")]
    public Task<ActionResult> Trade(
        [FromRoute] string positionId,
        [FromRoute] string ticker) =>

        this.OkOrError(
            _handler.Handle(
                new SimulateTrade(
                    ticker: new Ticker(ticker),
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)), 
                    userId: User.Identifier()
                )
            )
        );

    [HttpGet("{ticker}/positions/{positionId}/profitpoints")]
    public Task<ActionResult> ProfitPoints(
        [FromRoute] string positionId,
        [FromRoute] string ticker,
        [FromQuery] int numberOfPoints) =>

        this.OkOrError(
            _handler.Handle(
                new ProfitPointsQuery(
                    numberOfPoints: numberOfPoints,
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    ticker: new Ticker(ticker),
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{ticker}/positions/{positionId}/grade")]
    public Task Grade([FromBody]GradePosition command) =>
        this.OkOrError(
            _handler.HandleGradePosition(
                User.Identifier(), command
            )
        );
    
    [HttpPost("{ticker}/positions")]
    public Task<ActionResult> OpenLongPosition([FromBody]OpenLongStockPosition command) =>
        this.OkOrError(
            _handler.Handle(
                User.Identifier(), command
            )
        );

    [HttpDelete("{ticker}/positions/{positionId}")]
    public Task<ActionResult> DeletePosition(
        [FromRoute] string positionId,
        [FromRoute] string ticker,
        [FromServices] Handler handler) =>
        this.OkOrError(
            handler.Handle(
                new DeletePosition(
                    ticker: new Ticker(ticker),
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{ticker}/positions/{positionId}/labels")]
    public Task SetLabel([FromBody]AddLabel command, [FromServices] Handler handler) =>
        this.OkOrError(
            handler.HandleAddLabel(
                User.Identifier(), command
            )
        );
    
    [HttpPost("positions/{positionId}/stop")]
    public Task<ActionResult> Stop([FromBody] SetStop command) =>
        this.OkOrError(_handler.HandleStop(User.Identifier(), command));

    [HttpDelete("{ticker}/positions/{positionId}/labels/{label}")]
    public Task RemoveLabel(
        [FromRoute] string positionId,
        [FromRoute] string ticker,
        [FromRoute] string label,
        [FromServices] Handler handler) => 
        this.OkOrError(
            handler.Handle(
                new RemoveLabel(
                    ticker: new Ticker(ticker),
                    positionId: StockPositionId.NewStockPositionId(Guid.Parse(positionId)),
                    key: label,
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{ticker}/positions/{positionId}/risk")]
    public Task<ActionResult> Risk(SetRisk command) =>
        this.OkOrError(
            _handler.HandleSetRisk(
                User.Identifier(), command
            )
        );
    
    [HttpDelete("{ticker}/positions/{positionId}/transactions/{eventId}")]
    public Task<ActionResult> DeleteTransaction([FromRoute] string positionId, [FromRoute] Guid eventId) =>
        this.OkOrError(
            _handler.Handle(
                new DeleteTransaction(StockPositionId.NewStockPositionId(Guid.Parse(positionId)), User.Identifier(), eventId
                )
            )
        );
    
    [HttpDelete("{ticker}/positions/{positionId}/stop")]
    public async Task<ActionResult> DeleteStop([FromRoute] string positionId) =>
        this.OkOrError(await _handler.Handle(User.Identifier(), new DeleteStop(StockPositionId.NewStockPositionId(Guid.Parse(positionId)))));

    [HttpGet("{ticker}/simulate/trades")]
    public Task<ActionResult> Trade(
        string ticker,
        [FromQuery] decimal numberOfShares,
        [FromQuery] decimal price,
        [FromQuery] decimal stopPrice,
        [FromQuery] string when) =>

        this.OkOrError(
            _handler.Handle(
                new SimulateTradeForTicker(
                    userId: User.Identifier(),
                    ticker: new Ticker(ticker),
                    numberOfShares: numberOfShares,
                    price: price,
                    stopPrice: stopPrice,
                    date: DateTimeOffset.Parse(when))
            )
        );

    [HttpGet("tradingentries")]
    public Task<ActionResult> TradingEntries() =>
        this.OkOrError(
            _handler.Handle(
                new QueryTradingEntries(
                    User.Identifier()
                )
            )
        );
        
    [HttpGet("pasttradingentries")]
    public Task<ActionResult> PastTradingEntries() =>
        this.OkOrError(
            _handler.Handle(
                new QueryPastTradingEntries(
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
            
        return await this.OkOrError(_handler.Handle(cmd));
    }
}