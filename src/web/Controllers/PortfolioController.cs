using System;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class PortfolioController : ControllerBase
    {
        private IPortfolioStorage _storage;

        public PortfolioController(IPortfolioStorage storage)
        {
            this._storage = storage;
        }

        [HttpGet]
        public async Task<object> Stocks()
        {
            var stocks = await _storage.GetStocks(this.User.Identifier());

            var cashedout = stocks.Where(s => s.State.Owned == 0);
            var owned = stocks.Where(s => s.State.Owned > 0);

            var totalSpent = stocks.Sum(s => s.State.Spent);
            var totalEarned = stocks.Sum(s => s.State.Earned);

            var options = await _storage.GetSoldOptions(this.User.Identifier());

            var ownedOptions = options.Where(o => o.State.Amount > 0);
            var closedOptions = options.Where(o => o.State.Amount == 0);
            
            var obj = new
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

            return obj;
        }

        [HttpGet("stocks/export")]
        public async Task<ActionResult> StocksExport()
        {
            var stocks = await _storage.GetStocks(this.User.Identifier());

            var filename = CSVExport.GenerateFilename("stocks");

            this.HttpContext.Response.Headers.Add(
                "content-disposition", 
                $"attachment; filename={filename}");

            return new ContentResult
            {
                Content = CSVExport.Generate(stocks),
                ContentType = "text/csv"
            };
        }

        [HttpGet("options/export")]
        public async Task<ActionResult> OptionsExport()
        {
            var options = await _storage.GetSoldOptions(this.User.Identifier());

            var filename = CSVExport.GenerateFilename("options");

            this.HttpContext.Response.Headers.Add(
                "content-disposition", 
                $"attachment; filename={filename}");

            return new ContentResult
            {
                Content = CSVExport.Generate(options),
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
                amount = o.State.Amount,
                riskPct = o.State.Premium / (o.State.StrikePrice * 100) * 100,
                profit = o.State.Profit
            };
        }

        [HttpPost("purchase")]
        public async Task<ActionResult> PurchaseAsync(PurchaseModel model)
        {
            var stock = await this._storage.GetStock(model.Ticker, this.User.Identifier());

            if (stock == null)
            {
                stock = new OwnedStock(model.Ticker, this.User.Identifier());
            }

            stock.Purchase(model.Amount, model.Price, model.Date);

            await this._storage.Save(stock);

            return Ok();
        }

        [HttpPost("open")]
        public async Task<ActionResult> OpenAsync(OpenModel model)
        {
            var type = Enum.Parse<core.Portfolio.OptionType>(model.OptionType);

            var option = await this._storage.GetSoldOption(model.Ticker, type, model.ExpirationDate.Value, model.StrikePrice, this.User.Identifier());
            if (option == null)
            {
                option = new SoldOption(model.Ticker, type, model.ExpirationDate.Value, model.StrikePrice, this.User.Identifier());
            }

            option.Open(model.Amount, model.Premium, model.Filled.Value);

            await this._storage.Save(option);

            return Ok();
        }

        [HttpPost("sell")]
        public async Task<ActionResult> SellAsync(PurchaseModel model)
        {
            var stock = await this._storage.GetStock(model.Ticker, this.User.Identifier());

            if (stock == null)
            {
                return NotFound();
            }

            stock.Sell(model.Amount, model.Price, model.Date);

            await this._storage.Save(stock);

            return Ok();
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}")]
        public async Task<object> SoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration)
        {
            var sold = await _storage.GetSoldOption(ticker, Enum.Parse<core.Portfolio.OptionType>(type), expiration, strikePrice, this.User.Identifier());
            if (sold == null)
            {
                return NotFound();
            }

            return ToOptionView(sold);
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}/close")]
        public async Task<ActionResult> CloseSoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration, double closePrice, DateTimeOffset closeDate, int amount)
        {
            var sold = await _storage.GetSoldOption(ticker, Enum.Parse<core.Portfolio.OptionType>(type), expiration, strikePrice, this.User.Identifier());
            if (sold == null)
            {
                return NotFound();
            }

            sold.Close(amount, closePrice, closeDate);

            await _storage.Save(sold);

            return Ok();
        }
    }
}