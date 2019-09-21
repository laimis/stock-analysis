using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core.Portfolio;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class PortfolioController : Controller
    {
        private IPortfolioStorage _storage;
        const string _user = "laimonas";

        public PortfolioController(IPortfolioStorage storage)
        {
            this._storage = storage;
        }

        [HttpGet]
        public async Task<object> Stocks()
        {
            var stocks = await _storage.GetStocks(_user);

            var cashedout = stocks.Where(s => s.State.Owned == 0);
            var owned = stocks.Where(s => s.State.Owned > 0);

            var totalSpent = stocks.Sum(s => s.State.Spent);
            var totalEarned = stocks.Sum(s => s.State.Earned);

            var options = await _storage.GetSoldOptions(_user);

            var ownedOptions = options.Where(o => o.State.Amount > 0);
            var closedOptions = options.Where(o => o.State.Amount == 0);
            
            return new
            {
                totalSpent,
                totalEarned,
                totalCashedOutSpend = cashedout.Sum(s => s.State.Spent),
                totalCashedOutEarnings = cashedout.Sum(s => s.State.Earned),
                owned = owned.Select(o => ToOwnedView(o)),
                cashedOut = cashedout.Select(o => ToOwnedView(o)),
                ownedOptions = ownedOptions.Select(o => ToOptionView(o)),
                closedOptions = closedOptions.Select(o => ToOptionView(o)),
                pendingPremium = ownedOptions.Sum(o => o.State.Premium),
                collateralCash = ownedOptions.Sum(o => o.State.CollateralCash),
                collateralShares = ownedOptions.Sum(o => o.State.CollateralShares),
                optionEarnings = options.Sum(o => o.State.Profit)
            };
        }

        [HttpGet("csv")]
        public async Task<ActionResult> CSV()
        {
            var stocks = await _storage.GetStocks(_user);

            var builder = new StringBuilder();

            foreach (var s in stocks)
            {
                builder.AppendLine($"{s.State.Ticker},{s.State.Spent},{s.State.Earned}");
            }

            return new ContentResult
            {
                Content = builder.ToString(),
                ContentType = "text/csv"
            };
        }

        private object ToOwnedView(OwnedStock o)
        {
            return new
            {
                ticker = o.State.Ticker,
                owned = o.State.Owned,
                spent = Math.Round(o.State.Spent, 2),
                earned = Math.Round(o.State.Earned, 2)
            };
        }

        private object ToOptionView(SoldOption o)
        {
            return new
            {
                ticker = o.State.Ticker,
                type = o.State.Type.ToString(),
                strikePrice = o.State.StrikePrice,
                expiration = o.State.Expiration,
                premium = o.State.Premium,
                riskPct = o.State.Premium / (o.State.StrikePrice * 100) * 100,
                profit = o.State.Profit
            };
        }

        [HttpPost("purchase")]
        public async Task<ActionResult> PurchaseAsync([FromBody]PurchaseModel model)
        {
            var stock = await this._storage.GetStock(model.Ticker, _user);

            if (stock == null)
            {
                stock = new OwnedStock(model.Ticker, _user);
            }

            stock.Purchase(model.Amount, model.Price, model.Date);

            await this._storage.Save(stock);

            return Ok();
        }

        [HttpPost("open")]
        public async Task<ActionResult> OpenAsync([FromBody]OpenModel model)
        {
            var type = Enum.Parse<core.Portfolio.OptionType>(model.OptionType.ToString());

            var option = await this._storage.GetSoldOption(model.Ticker, type, model.Expiration, model.StrikePrice, _user);
            if (option == null)
            {
                option = new SoldOption(model.Ticker, type, model.Expiration, model.StrikePrice, _user);
            }

            option.Open(model.Amount, model.Premium, model.Filled);

            await this._storage.Save(option);

            return Ok();
        }

        [HttpPost("sell")]
        public async Task<ActionResult> SellAsync([FromBody]PurchaseModel model)
        {
            var stock = await this._storage.GetStock(model.Ticker, _user);

            if (stock == null)
            {
                return NotFound();
            }

            stock.Sell(model.Amount, model.Price, model.Date);

            await this._storage.Save(stock);

            return Ok();
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}")]
        public async Task<ActionResult> SoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration)
        {
            var sold = await _storage.GetSoldOption(ticker, Enum.Parse<core.Portfolio.OptionType>(type), expiration, strikePrice, _user);
            if (sold == null)
            {
                return NotFound();
            }

            return Json(ToOptionView(sold));
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}/close")]
        public async Task<ActionResult> CloseSoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration, double closePrice, DateTimeOffset closeDate)
        {
            var sold = await _storage.GetSoldOption(ticker, Enum.Parse<core.Portfolio.OptionType>(type), expiration, strikePrice, _user);
            if (sold == null)
            {
                return NotFound();
            }

            sold.Close(1, closePrice, closeDate);

            await _storage.Save(sold);

            return Ok();
        }
    }
}