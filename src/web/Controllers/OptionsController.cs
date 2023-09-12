using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Options;
using core.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using Delete = core.fs.Options.Delete;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        [HttpGet("{ticker}/chain")]
        public Task<ActionResult> Chain([FromRoute] string ticker, [FromServices] Chain.Handler service) =>
            this.OkOrError(
                service.Handle(
                    new Chain.Query(
                        ticker: ticker, userId: User.Identifier()
                    )
                )
            );
        
        [HttpGet("{optionId:guid}")]
        public Task<ActionResult> Get([FromRoute] Guid optionId, [FromServices] Details.Handler service) =>
            this.OkOrError(
                service.Handle(
                    new Details.Query(optionId: optionId, userId: User.Identifier()
                    )
                )
            );

        [HttpPost("sell")]
        public Task<ActionResult> Sell([FromBody]OptionTransaction cmd, [FromServices] BuyOrSell.Handler service)
        {
            cmd.UserId = User.Identifier();

            return this.OkOrError(
                service.Handle(
                    BuyOrSell.Command.NewSell(cmd)
                )
            );
        }

        [HttpPost("buy")]
        public Task<ActionResult> Buy([FromBody]OptionTransaction cmd, [FromServices] BuyOrSell.Handler service)
        {
            cmd.UserId = User.Identifier();

            return this.OkOrError(
                service.Handle(
                    BuyOrSell.Command.NewBuy(cmd)
                )
            );
        }

        [HttpDelete("{id}")]
        public Task<ActionResult> Delete([FromServices]Delete.Handler service, [FromRoute] Guid id)
            => this.OkOrError(
                service.Handle(
                    new Delete.Command(id, User.Identifier())
                )
            );

        [HttpPost("{optionId}/expire")]
        public Task<ActionResult> Expire([FromServices]Expire.Handler service, [FromRoute] Guid optionId)
            => this.OkOrError(
                service.Handle(
                    core.fs.Options.Expire.Command.NewExpire(
                        new Expire.ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpPost("{optionId}/assign")]
        public Task<ActionResult> Assign([FromServices]Expire.Handler service, [FromRoute] Guid optionId)
            => this.OkOrError(
                service.Handle(
                    core.fs.Options.Expire.Command.NewAssign(
                        new Expire.ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpGet("export")]
        public async Task<ActionResult> Export([FromServices]Export.Handler service)
        {
            return this.GenerateExport(
                await service.Handle(
                    new Export.Query(
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
        public Task<ActionResult> Dashboard([FromServices]Dashboard.Handler service)
            => this.OkOrError(
                service.Handle(
                    new Dashboard.Query(
                        User.Identifier()
                    )
                ));
    }
}