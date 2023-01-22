using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Portfolio;
using core.Portfolio.Handlers;
using core.Portfolio.Output;
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

        [HttpGet("stocklists/{name}")]
        public Task<StockListState> GetStockList(string name) =>
            _mediator.Send(new ListsGet.Query(name, User.Identifier()));

        [HttpGet("stocklists/{name}/export")]
        public Task<ActionResult> ExportStockList(string name, [FromQuery]bool justTickers) =>
            this.GenerateExport(_mediator, new ListsExport.Query(justTickers, name, User.Identifier()));

        [HttpGet]
        public Task<PortfolioResponse> Index() =>
            _mediator.Send(new Get.Query(User.Identifier()));

        [HttpGet("transactions")]
        public Task<TransactionList> TransactionsAsync(string ticker, string groupBy, string show, string txType) =>
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

        [HttpGet("simulate/trades/{ticker}/positions/{positionId}")]
        public Task<TradingStrategyResults> Trade(
            int positionId,
            string ticker) =>
            
            _mediator.Send(
                new SimulateTrade.Command(
                    positionId, ticker, User.Identifier()
                )
            );

        [HttpGet("profitpoints/{ticker}/positions/{positionId}")]
        public Task<core.Stocks.Services.Trading.ProfitPoints.ProfitPointContainer[]> ProfitPoints(
            int positionId,
            string ticker) =>

            _mediator.Send(
                new core.Portfolio.ProfitPoints.Query(
                    positionId, ticker, User.Identifier()
                )
            );

        [HttpGet("simulate/trades/{ticker}")]
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
    }
}