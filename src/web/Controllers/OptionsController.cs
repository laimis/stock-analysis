using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Adapters.Options;
using core.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        private IOptionsService _options;
        private IPortfolioStorage _storage;
        private IMediator _mediator;

        public OptionsController(
            IOptionsService options,
            IPortfolioStorage storage,
            IMediator mediator)
        {
            _options = options;
            _storage = storage;
            _mediator = mediator;
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
        public async Task<ActionResult> Sell(SellOption.Command cmd)
        {
            cmd.WithUser(this.User.Identifier());
            
            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}")]
        public async Task<object> SoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration)
        {
            var sold = await _storage.GetSoldOption(ticker, Enum.Parse<OptionType>(type), expiration, strikePrice, this.User.Identifier());
            if (sold == null)
            {
                return NotFound();
            }

            return ToOptionView(sold);
        }

        [HttpPost("close")]
        public async Task<ActionResult> CloseSoldOption(CloseOption cmd)
        {
            var sold = await _storage.GetSoldOption(
                cmd.Ticker,
                Enum.Parse<OptionType>(cmd.OptionType),
                cmd.Expiration.Value,
                cmd.StrikePrice,
                this.User.Identifier());

            if (sold == null)
            {
                return NotFound();
            }

            sold.Close(cmd.Amount, cmd.ClosePrice.Value, cmd.CloseDate.Value);

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
                expiration = o.State.Expiration.ToString("yyyy-MM-dd"),
                premium = o.State.Premium,
                amount = o.State.Amount,
                riskPct = o.State.Premium / (o.State.StrikePrice * 100) * 100,
                profit = o.State.Profit
            };
        }
    }
}