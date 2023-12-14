using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Options;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        private readonly Handler _service;

        public OptionsController(Handler service)
        {
            _service = service;
        }
        
        [HttpGet("chain/{ticker}")]
        public Task<ActionResult> Chain([FromRoute] string ticker) =>
            this.OkOrError(
                _service.Handle(
                    new ChainQuery(
                        ticker: new Ticker(ticker), userId: User.Identifier()
                    )
                )
            );
        
        [HttpGet("ownership/{ticker}")]
        public Task<ActionResult> Ownership([FromRoute] string ticker) =>
            this.OkOrError(
                _service.Handle(
                    new OwnershipQuery(
                        ticker: new Ticker(ticker), userId: User.Identifier()
                    )
                )
            );
        
        [HttpGet("{optionId:guid}")]
        public Task<ActionResult> Get([FromRoute] Guid optionId) =>
            this.OkOrError(
                _service.Handle(
                    new DetailsQuery(optionId: optionId, userId: User.Identifier()
                    )
                )
            );

        [HttpPost("sell")]
        public Task<ActionResult> Sell([FromBody]OptionTransaction cmd)
        {
            return this.OkOrError(
                _service.Handle(
                    BuyOrSellCommand.NewSell(cmd, User.Identifier())
                )
            );
        }

        [HttpPost("buy")]
        public Task<ActionResult> Buy([FromBody]OptionTransaction cmd)
        {
            return this.OkOrError(
                _service.Handle(
                    BuyOrSellCommand.NewBuy(cmd, User.Identifier())
                )
            );
        }

        [HttpDelete("{id}")]
        public Task<ActionResult> Delete([FromRoute] Guid id)
            => this.OkOrError(
                _service.Handle(
                    new DeleteCommand(id, User.Identifier())
                )
            );

        [HttpPost("{optionId}/expire")]
        public Task<ActionResult> Expire([FromRoute] Guid optionId)
            => this.OkOrError(
                _service.Handle(
                    ExpireCommand.NewExpire(
                        new ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpPost("{optionId}/assign")]
        public Task<ActionResult> Assign([FromRoute] Guid optionId)
            => this.OkOrError(
                _service.Handle(
                    ExpireCommand.NewAssign(
                        new ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(
                _service.Handle(
                    new ExportQuery(
                        User.Identifier()
                    )
                )
            );
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file, [FromServices]Import.Handler service, CancellationToken token)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync(token);

            return await this.OkOrError(
                service.Handle(
                    new Import.Command(content, User.Identifier()),
                    token
                )
            );
        }

        [HttpGet]
        public Task<ActionResult> Dashboard()
            => this.OkOrError(
                _service.Handle(
                    new DashboardQuery(
                        User.Identifier()
                    )
                ));
    }
}