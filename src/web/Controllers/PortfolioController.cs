using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Portfolio;
using core.Portfolio.Handlers;
using core.Portfolio.Views;
using core.Stocks;
using core.Stocks.Services.Trading;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private IMediator _mediator;

        public PortfolioController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("pendingstockpositions")]
        public Task<IEnumerable<PendingStockPositionState>> PendingStockPositions() =>
            _mediator.Send(new PendingStockPositionsGet.Query(User.Identifier()));

        [HttpPost("pendingstockpositions")]
        public Task<PendingStockPositionState> CreatePendingStockPosition([FromBody]PendingStockPositionCreate.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("pendingstockpositions/{id}")]
        public Task DeletePendingStockPosition(Guid id) =>
            _mediator.Send(new PendingStockPositionClose.Command(id, User.Identifier()));

        [HttpGet("routines")]
        public Task<RoutineState[]> Routines() =>
            _mediator.Send(new Routines.Query(User.Identifier()));

        [HttpPost("routines")]
        public Task<RoutineState> CreateRoutine([FromBody]RoutinesCreate.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpPut("routines/{routineName}")]
        public Task<RoutineState> AddRoutineStep([FromBody]RoutinesAddStep.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("routines/{routineName}")]
        public Task DeleteRoutine(string routineName) =>
            _mediator.Send(new RoutinesDelete.Command(routineName, User.Identifier()));
        
        [HttpPost("routines/{routineName}/{stepIndex}")]
        public Task<RoutineState> UpdateRoutineStep(RoutinesUpdateStep.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return _mediator.Send(cmd);
        }

        [HttpDelete("routines/{routineName}/{stepIndex}")]
        public Task<RoutineState> RemoveRoutineStep(string routineName, int stepIndex) =>
            _mediator.Send(new RoutinesRemoveStep.Command(routineName, stepIndex, User.Identifier()));


        [HttpGet("stocklists")]
        public Task<StockListState[]> StockLists() =>
            _mediator.Send(new Lists.Query(User.Identifier()));

        [HttpPost("stocklists")]
        public Task<StockListState> CreateStockList([FromBody]ListsCreate.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("stocklists/{name}")]
        public Task DeleteStockList(string name) =>
            _mediator.Send(new ListsDelete.Command(name, User.Identifier()));

        [HttpPut("stocklists/{name}")]
        public Task<StockListState> AddStockToList([FromBody]ListsAddStock.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("stocklists/{name}/{ticker}")]
        public Task<StockListState> RemoveStockFromList(string name, string ticker) =>
            _mediator.Send(new ListsRemoveStock.Command(name, ticker, User.Identifier()));

        [HttpPut("stocklists/{name}/tags")]
        public Task<StockListState> AddTagToStockList([FromBody]ListsAddTag.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("stocklists/{name}/tags/{tag}")]
        public Task<StockListState> RemoveTagFromStockList(string name, string tag) =>
            _mediator.Send(new ListsRemoveTag.Command(name, tag, User.Identifier()));

        [HttpGet("stocklists/{name}")]
        public Task<StockListState> GetStockList(string name) =>
            _mediator.Send(new ListsGet.Query(name, User.Identifier()));

        [HttpGet("stocklists/{name}/export")]
        public Task<ActionResult> ExportStockList(string name, [FromQuery]bool justTickers) =>
            this.GenerateExport(_mediator, new ListsExport.Query(justTickers, name, User.Identifier()));

        [HttpGet]
        public Task<PortfolioView> Index() =>
            _mediator.Send(new Get.Query(User.Identifier()));

        [HttpGet("transactions")]
        public Task<TransactionsView> TransactionsAsync(string ticker, string groupBy, string show, string txType) =>
            _mediator.Send(
                new Transactions.Query(User.Identifier(), ticker, groupBy, show, txType)
            );

        [HttpGet("transactionsummary")]
        public Task<TransactionSummaryView> Review(string period) =>
            _mediator.Send(
                new TransactionSummary.Generate(period, User.Identifier())
            );

        [HttpGet("simulate/trades")]
        public Task<List<TradingStrategyPerformance>> Trade(
            [FromQuery]bool closePositionIfOpenAtTheEnd,
            [FromQuery]int numberOfTrades) =>
            
            _mediator.Send(
                new SimulateTrades.Query(
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades,
                    userId: User.Identifier()
                )
            );

        [HttpGet("simulate/trades/export")]
        public Task<ActionResult> SimulateTradesExport(
            [FromQuery]bool closePositionIfOpenAtTheEnd,
            [FromQuery]int numberOfTrades) =>
            
            this.GenerateExport(
                _mediator,
                new SimulateTrades.ExportQuery(
                    closePositionIfOpenAtTheEnd: closePositionIfOpenAtTheEnd,
                    numberOfTrades: numberOfTrades,
                    userId: User.Identifier()
                )
            );

        [HttpGet("{ticker}/positions/{positionId}/simulate/trades")]
        public Task<TradingStrategyResults> Trade(
            int positionId,
            string ticker) =>
            
            _mediator.Send(
                new SimulateTrade.ForPosition(
                    positionId, ticker, User.Identifier()
                )
            );

        [HttpGet("{ticker}/positions/{positionId}/profitpoints")]
        public Task<core.Stocks.Services.Trading.ProfitPoints.ProfitPointContainer[]> ProfitPoints(
            int positionId,
            string ticker) =>

            _mediator.Send(
                new core.Portfolio.ProfitPoints.Query(
                    positionId, ticker, User.Identifier()
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/grade")]
        public Task Grade(
            int positionId,
            string ticker,
            [FromBody]GradePosition.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("{ticker}/positions/{positionId}")]
        public Task DeletePosition(
            int positionId,
            string ticker) => _mediator.Send(
                new DeletePosition.Command(
                    positionId, ticker, User.Identifier()
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/labels")]
        public Task SetLabel(
            int positionId,
            string ticker,
            [FromBody]PositionLabelsSet.Command command)
        {
            command.WithUserId(User.Identifier());

            return _mediator.Send(command);
        }

        [HttpDelete("{ticker}/positions/{positionId}/labels/{label}")]
        public Task RemoveLabel(
            int positionId,
            string ticker,
            string label) => _mediator.Send(
                new PositionLabelsDelete.Command(
                    ticker, positionId, label, User.Identifier()
                )
            );

        [HttpPost("{ticker}/positions/{positionId}/risk")]
        public async Task<ActionResult> Risk(SetRisk.Command model)
        {
            model.WithUserId(User.Identifier());

            var r = await _mediator.Send(model);

            return this.OkOrError(r);
        }

        

        [HttpGet("{ticker}/simulate/trades")]
        public Task<TradingStrategyResults> Trade(
            string ticker,
            [FromQuery]decimal numberOfShares,
            [FromQuery]decimal price,
            [FromQuery]decimal stopPrice,
            [FromQuery]string when) =>
            
            _mediator.Send(
                new SimulateTrade.ForTicker(
                    DateTimeOffset.Parse(when),
                    numberOfShares,
                    price,
                    stopPrice,
                    ticker,
                    User.Identifier()
                )
            );

        [HttpGet("tradingentries")]
        public Task<TradingEntriesView> TradingEntries() =>
            _mediator.Send(
                new TradingEntries.Query(User.Identifier())
            );
    }
}