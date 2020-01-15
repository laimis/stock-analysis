using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Options;
using core.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class OptionsController : Controller
    {
        private IOptionsService _options;
        private IPortfolioStorage _storage;

        public OptionsController(
            IOptionsService options,
            IPortfolioStorage storage)
        {
            _options = options;
            _storage = storage;
        }

        [HttpGet("{ticker}")]
        public async Task<ActionResult<OptionDetailsViewModel>> DetailsAsync(string ticker)
        {
            var price = await _options.GetPrice(ticker);
            if (price.NotFound)
            {
                return NotFound();
            }
            
            var dates = await _options.GetOptions(ticker);
            
            var upToFour = dates.Take(4);

            var options = new List<OptionDetail>();

            foreach(var d in upToFour)
            {
                var details = await _options.GetOptionDetails(ticker, d);
                options.AddRange(details);
            }

            return OptionDetailsViewModelMapper.Map(price.Amount, options);
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(SellOption cmd)
        {
            var type = Enum.Parse<core.Portfolio.OptionType>(cmd.OptionType);

            var option = await this._storage.GetSoldOption(cmd.Ticker, type, cmd.ExpirationDate.Value, cmd.StrikePrice, this.User.Identifier());
            if (option == null)
            {
                option = new SoldOption(cmd.Ticker, type, cmd.ExpirationDate.Value, cmd.StrikePrice, this.User.Identifier());
            }

            option.Open(cmd.Amount, cmd.Premium, cmd.Filled.Value);

            await this._storage.Save(option);

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

        [HttpGet("export")]
        public async Task<ActionResult> Export()
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

        internal static object ToOptionView(SoldOption o)
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
    }
}