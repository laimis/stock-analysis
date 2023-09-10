using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Options;
using core.Options;
using MediatR;
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
        private readonly IMediator _mediator;
        
        public OptionsController(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{ticker}/chain")]
        public Task<ActionResult> DetailsAsync(string ticker) =>
            this.ExecuteAsync(
                _mediator,
                new Chain.Query(ticker, User.Identifier()
            )
        );

        [HttpGet("{ticker}/active")]
        public Task<ActionResult> List(string ticker) =>
            this.ExecuteAsync(_mediator, new List.Query(ticker, User.Identifier()));

        [HttpGet("{id}")]
        public Task<ActionResult> Get(Guid id) =>
            this.ExecuteAsync(_mediator, new Details.Query(id, User.Identifier()));

        [HttpPost("sell")]
        public Task<ActionResult> Sell([FromBody]OptionTransaction cmd, [FromServices] BuyOrSell.Handler service)
        {
            cmd.WithUserId(User.Identifier());

            return this.OkOrError(
                service.Handle(
                    BuyOrSell.Command.NewBuy(cmd)
                )
            );
        }

        [HttpPost("buy")]
        public Task<ActionResult> Buy([FromBody]OptionTransaction cmd, [FromServices] BuyOrSell.Handler service)
        {
            cmd.WithUserId(User.Identifier());

            return this.OkOrError(
                service.Handle(
                    BuyOrSell.Command.NewSell(cmd)
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