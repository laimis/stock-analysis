using System;
using System.Threading.Tasks;
using core.fs.Portfolio;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using PendingPositions = core.fs.Portfolio.PendingPositions;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    [HttpGet("pendingstockpositions")]
    public Task<ActionResult> PendingStockPositions([FromServices]PendingPositions.Handler service) =>
        this.OkOrError(service.Handle(new PendingPositions.Query(User.Identifier())));

        
    [HttpGet("pendingstockpositions/export")]
    public Task<ActionResult> ExportPendingStockPositions([FromServices]PendingPositions.Handler service) =>
        this.GenerateExport(
            service.Handle(new PendingPositions.Export(User.Identifier())
            ));
        
    [HttpPost("pendingstockpositions")]
    public Task<ActionResult> CreatePendingStockPosition([FromBody]PendingPositions.Create command, [FromServices]PendingPositions.Handler service) =>
        this.OkOrError(
            service.HandleCreate(
                User.Identifier(), command
            )
        );

    [HttpDelete("pendingstockpositions/{id}")]
    public Task<ActionResult> ClosePendingStockPosition([FromRoute] Guid id,
        [FromServices] PendingPositions.Handler service) =>
        this.OkOrError(service.Handle(new PendingPositions.Close(id, User.Identifier())));

    [HttpGet]
    public Task<ActionResult> Index([FromServices] Handler service) =>
        this.OkOrError(service.Handle(new Query(User.Identifier())));

    [HttpGet("transactions")]
    public Task<ActionResult> Transactions(string ticker, string groupBy, string show, string txType,
        [FromServices] Handler service) =>
        this.OkOrError(
            service.Handle(
                new QueryTransactions(
                    userId: User.Identifier(),
                    show: show,
                    groupBy: groupBy,
                    txType: txType,
                    ticker: new Ticker(ticker))
            )
        );

    [HttpGet("transactionsummary")]
    public Task<ActionResult> Review(string period, [FromServices] Handler service) =>
        this.OkOrError(service.Handle(new TransactionSummary(period: period, userId: User.Identifier())));

    [HttpGet("simulate/trades")]
    public Task<ActionResult> Trade(
        [FromQuery] bool closePositionIfOpenAtTheEnd,
        [FromQuery] int numberOfTrades,
        [FromServices] Handler service) =>

        this.OkOrError(
            service.Handle(
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
        [FromQuery] int numberOfTrades,
        [FromServices] Handler service) =>

        this.GenerateExport(
            service.HandleExport(
                new ExportUserSimulatedTrades(
                    userId: User.Identifier(),
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades
                )
            )
        );

    [HttpGet("{ticker}/positions/{positionId}/simulate/trades")]
    public Task<ActionResult> Trade(
        [FromRoute] int positionId,
        [FromRoute] string ticker,
        [FromServices] Handler service) =>

        this.OkOrError(
            service.Handle(
                new SimulateTrade(
                    ticker: new Ticker(ticker),
                    positionId: positionId, 
                    userId: User.Identifier()
                )
            )
        );

    [HttpGet("{ticker}/positions/{positionId}/profitpoints")]
    public Task<ActionResult> ProfitPoints(
        [FromRoute] int positionId,
        [FromRoute] string ticker,
        [FromQuery] int numberOfPoints,
        [FromServices] Handler service) =>

        this.OkOrError(
            service.Handle(
                new ProfitPointsQuery(
                    numberOfPoints: numberOfPoints,
                    positionId: positionId,
                    ticker: new Ticker(ticker),
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{ticker}/positions/{positionId}/grade")]
    public Task Grade([FromBody]GradePosition command, [FromServices]Handler service) =>
        this.OkOrError(
            service.HandleGradePosition(
                User.Identifier(), command
            )
        );

    [HttpDelete("{ticker}/positions/{positionId}")]
    public Task<ActionResult> DeletePosition(
        [FromRoute] int positionId,
        [FromRoute] string ticker,
        [FromServices] Handler handler) =>
        this.OkOrError(
            handler.Handle(
                new DeletePosition(
                    ticker: new Ticker(ticker),
                    positionId: positionId,
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

    [HttpDelete("{ticker}/positions/{positionId}/labels/{label}")]
    public Task RemoveLabel(
        [FromRoute] int positionId,
        [FromRoute] string ticker,
        [FromRoute] string label,
        [FromServices] Handler handler) => 
        this.OkOrError(
            handler.Handle(
                new RemoveLabel(
                    ticker: new Ticker(ticker),
                    positionId: positionId,
                    key: label,
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{ticker}/positions/{positionId}/risk")]
    public Task<ActionResult> Risk(SetRisk command, [FromServices] Handler service) =>
        this.OkOrError(
            service.HandleSetRisk(
                User.Identifier(), command
            )
        );

    [HttpGet("{ticker}/simulate/trades")]
    public Task<ActionResult> Trade(
        string ticker,
        [FromQuery] decimal numberOfShares,
        [FromQuery] decimal price,
        [FromQuery] decimal stopPrice,
        [FromQuery] string when,
        [FromServices] Handler service) =>

        this.OkOrError(
            service.Handle(
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
    public Task<ActionResult> TradingEntries([FromServices] Handler service) =>
        this.OkOrError(
            service.Handle(
                new QueryTradingEntries(
                    User.Identifier()
                )
            )
        );
        
    [HttpGet("pasttradingentries")]
    public Task<ActionResult> PastTradingEntries([FromServices] Handler service) =>
        this.OkOrError(
            service.Handle(
                new QueryPastTradingEntries(
                    User.Identifier()
                )
            )
        );
}