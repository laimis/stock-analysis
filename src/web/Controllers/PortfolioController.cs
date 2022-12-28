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

        [HttpGet("{ticker}/positions/{positionId}/simulate/{stategyName}")]
        public Task<TradingStrategyResults> Trade(
            int positionId,
            string strategyName,
            string ticker) =>
            
            _mediator.Send(
                new SimulateTrade.Command(
                    positionId, strategyName, ticker, User.Identifier()
                )
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

        [HttpGet("{ticker}/trading/{stategyName}")]
        public Task<TradingStrategyResults> Trade(
            string strategyName,
            string ticker,
            decimal numberOfShares,
            decimal price,
            decimal stopPrice,
            string when) =>
            
            _mediator.Send(
                new SimulateTrade.ForTicker(
                    DateTimeOffset.Parse(when),
                    numberOfShares,
                    price,
                    stopPrice,
                    strategyName,
                    ticker,
                    User.Identifier()
                )
            );
    }
}