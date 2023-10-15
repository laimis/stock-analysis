using System;
using System.Threading.Tasks;
using core.fs.Portfolio;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using PendingPositions = core.fs.Portfolio.PendingPositions;
using Routines = core.fs.Portfolio.Routines;
using Lists = core.fs.Portfolio.Lists;
namespace web.Controllers
{
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
                service.Handle(
                    PendingPositions.Create.WithUserId(User.Identifier(), command)
                )
            );

        [HttpDelete("pendingstockpositions/{id}")]
        public Task<ActionResult> ClosePendingStockPosition([FromRoute] Guid id,
            [FromServices] PendingPositions.Handler service) =>
            this.OkOrError(service.Handle(new PendingPositions.Close(id, User.Identifier())));

        [HttpGet("routines")]
        public Task<ActionResult> GetRoutines([FromServices] Routines.Handler service) =>
            this.OkOrError(service.Handle(new Routines.Query(User.Identifier())));

        [HttpPost("routines")]
        public Task<ActionResult> CreateRoutine([FromBody] Routines.Create command,
            [FromServices] Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    Routines.Create.WithUserId(User.Identifier(), command)
                )
            );

        [HttpPut("routines/{routineName}")]
        public Task UpdateRoutine([FromBody]Routines.Update command, [FromServices]Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    Routines.Update.WithUserId(User.Identifier(), command)
                )
            );

        [HttpDelete("routines/{routineName}")]
        public Task DeleteRoutine([FromRoute]string routineName, [FromServices]Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    new Routines.Delete(User.Identifier(), routineName)
                )
            );

        [HttpPut("routines/{routineName}/steps")]
        public Task<ActionResult> AddRoutineStep([FromBody]Routines.AddStep command, [FromServices]Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    Routines.AddStep.WithUserId(User.Identifier(), command)
                )
            );
        
        [HttpPost("routines/{routineName}/steps/{stepIndex}")]
        public Task<ActionResult> UpdateRoutineStep([FromBody]Routines.UpdateStep command, [FromServices]Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    Routines.UpdateStep.WithUserId(User.Identifier(), command)
                )
            );


        [HttpDelete("routines/{routineName}/steps/{stepIndex}")]
        public Task<ActionResult> RemoveRoutineStep([FromRoute] string routineName, [FromRoute] int stepIndex,
            [FromServices] Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    new Routines.RemoveStep(routineName, stepIndex, User.Identifier()
                    )
                )
            );

        [HttpPost("routines/{routineName}/steps/{stepIndex}/position")]
        public Task<ActionResult> MoveRoutineStep([FromBody] Routines.MoveStep cmd,
            [FromServices] Routines.Handler service) =>
            this.OkOrError(
                service.Handle(
                    Routines.MoveStep.WithUserId(User.Identifier(), cmd)
                )
            );

        [HttpGet("stocklists")]
        public Task<ActionResult> StockLists([FromServices]Lists.Handler service) =>
            this.OkOrError(service.Handle(new Lists.GetLists(User.Identifier())));

        [HttpPost("stocklists")]
        public Task<ActionResult> CreateStockList([FromBody]Lists.Create command, [FromServices]Lists.Handler service) =>
            this.OkOrError(service.Handle(Lists.Create.WithUserId(User.Identifier(), command)));

        [HttpDelete("stocklists/{name}")]
        public Task<ActionResult> DeleteStockList([FromRoute]string name, [FromServices]Lists.Handler service) =>
            this.OkOrError(service.Handle(new Lists.Delete(name, User.Identifier())));

        [HttpPost("stocklists/{name}")]
        public Task UpdateStockList([FromBody]Lists.Update command, [FromServices]Lists.Handler service) =>
            this.OkOrError(service.Handle(Lists.Update.WithUserId(User.Identifier(), command)));

        [HttpPut("stocklists/{name}")]
        public Task<ActionResult> AddStockToList([FromBody] Lists.AddStockToList command,
            [FromServices] Lists.Handler service) =>
            this.OkOrError(service.Handle(Lists.AddStockToList.WithUserId(User.Identifier(), command)));

        [HttpDelete("stocklists/{name}/{ticker}")]
        public Task<ActionResult> RemoveStockFromList([FromRoute] string name, [FromRoute] string ticker,
            [FromServices] Lists.Handler service) =>
            this.OkOrError(service.Handle(new Lists.RemoveStockFromList(name, User.Identifier(), new Ticker(ticker))));

        [HttpPut("stocklists/{name}/tags")]
        public Task<ActionResult> AddTagToStockList([FromBody]Lists.AddTagToList command, [FromServices]Lists.Handler service) =>
            this.OkOrError(service.Handle(Lists.AddTagToList.WithUserId(User.Identifier(), command)));

        [HttpDelete("stocklists/{name}/tags/{tag}")]
        public Task<ActionResult> RemoveTagFromStockList([FromRoute] string name, [FromRoute] string tag, [FromServices] Lists.Handler service) =>
            this.OkOrError(service.Handle(new Lists.RemoveTagFromList(tag, name, User.Identifier())));

        [HttpGet("stocklists/{name}")]
        public Task<ActionResult> GetStockList([FromRoute] string name, [FromServices] Lists.Handler service) =>
            this.OkOrError(service.Handle(new Lists.GetList(name, User.Identifier())));

        [HttpGet("stocklists/{name}/export")]
        public Task<ActionResult> ExportStockList(string name, [FromQuery] bool justTickers,
            [FromServices] Lists.Handler handler) =>
            this.GenerateExport(
                handler.Handle(
                    new Lists.ExportList(
                        justTickers: justTickers,
                        name: name,
                        userId: User.Identifier()
                    )
                )
            );

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
                        ticker: ticker)
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
                        ticker: ticker,
                        positionId: positionId, 
                        userId: User.Identifier()
                    )
                )
            );

        [HttpGet("{ticker}/positions/{positionId}/profitpoints")]
        public Task<ActionResult> ProfitPoints(
            [FromRoute] int positionId,
            [FromRoute] string ticker,
            [FromServices] Handler service) =>

            this.OkOrError(
                service.Handle(
                    new ProfitPointsQuery(
                        positionId: positionId,
                        ticker: new Ticker(ticker),
                        userId: User.Identifier()
                    )
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/grade")]
        public Task Grade([FromBody]GradePosition command, [FromServices]Handler service) =>
            this.OkOrError(
                service.Handle(
                    GradePosition.WithUserId(User.Identifier(), command)
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
                        ticker: ticker,
                        positionId: positionId,
                        userId: User.Identifier()
                    )
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/labels")]
        public Task SetLabel([FromBody]AddLabel command, [FromServices] Handler handler) =>
            this.OkOrError(
                handler.Handle(
                    AddLabel.WithUserId(User.Identifier(), command)
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
                        ticker: ticker,
                        positionId: positionId,
                        key: label,
                        userId: User.Identifier()
                    )
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/risk")]
        public Task<ActionResult> Risk(SetRisk command, [FromServices] Handler service) =>
            this.OkOrError(
                service.Handle(
                    SetRisk.WithUserId(User.Identifier(), command)
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
}