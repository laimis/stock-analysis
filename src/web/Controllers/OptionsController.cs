using System.Threading.Tasks;
using core.fs.Options;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OptionsController(OptionsHandler handler) : ControllerBase
    {
        [HttpGet("pricing")]
        public Task<ActionResult> GetOptionPricing([FromQuery] string symbol) =>
            this.OkOrError(
                handler.Handle(
                    new OptionPricingQuery(
                        User.Identifier(), OptionTicker.NewOptionTicker(symbol)
                    )
                )
            );
        
        [HttpGet("chain/{ticker}")]
        public Task<ActionResult> Chain([FromRoute] string ticker) =>
            this.OkOrError(
                handler.Handle(
                    new ChainQuery(
                        ticker: new Ticker(ticker), userId: User.Identifier()
                    )
                )
            );
        
        [HttpGet("export")]
        public async Task<ActionResult> Export()
        {
            return this.GenerateExport(
                await handler.Handle(
                    new ExportQuery(
                        User.Identifier()
                    )
                )
            );
        }
    }
}
