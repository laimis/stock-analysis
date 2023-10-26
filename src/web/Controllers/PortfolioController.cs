using System;
using System.Threading.Tasks;
using core.fs.Portfolio;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

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
        [FromRoute] int positionId,
        [FromRoute] string ticker) =>

        this.OkOrError(
            _handler.Handle(
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
        [FromQuery] int numberOfPoints) =>

        this.OkOrError(
            _handler.Handle(
                new ProfitPointsQuery(
                    numberOfPoints: numberOfPoints,
                    positionId: positionId,
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
    public Task<ActionResult> Risk(SetRisk command) =>
        this.OkOrError(
            _handler.HandleSetRisk(
                User.Identifier(), command
            )
        );

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
}