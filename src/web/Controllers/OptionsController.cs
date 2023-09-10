using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using MediatR;
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
        public Task<ActionResult> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return this.ExecuteAsync(_mediator, cmd);
        }

        [HttpPost("buy")]
        public Task<ActionResult> Buy(Buy.Command cmd)
        {
            cmd.WithUserId(User.Identifier());
            
            return this.ExecuteAsync(_mediator, cmd);
        }

        [HttpDelete("{id}")]
        public Task<ActionResult> Delete([FromServices]core.fs.Options.Delete.Handler service, [FromRoute] Guid id)
            => this.OkOrError(
                service.Handle(
                    new core.fs.Options.Delete.Command(id, User.Identifier())
                )
            );

        [HttpPost("{optionId}/expire")]
        public Task<ActionResult> Expire([FromServices]core.fs.Options.Expire.Handler service, [FromRoute] Guid optionId)
            => this.OkOrError(
                service.Handle(
                    core.fs.Options.Expire.Command.NewExpire(
                        new core.fs.Options.Expire.ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpPost("{optionId}/assign")]
        public Task<ActionResult> Assign([FromServices]core.fs.Options.Expire.Handler service, [FromRoute] Guid optionId)
            => this.OkOrError(
                service.Handle(
                    core.fs.Options.Expire.Command.NewAssign(
                        new core.fs.Options.Expire.ExpireData(userId: User.Identifier(), optionId: optionId)
                    )
                )
            );
        
        [HttpGet("export")]
        public async Task<ActionResult> Export([FromServices]core.fs.Options.Export.Handler service)
        {
            return this.GenerateExport(
                await service.Handle(
                    new core.fs.Options.Export.Query(
                        User.Identifier()
                    )
                )
            );
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file, [FromServices]core.fs.Options.Import.Handler service, CancellationToken token)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync(token);

            return await this.OkOrError(
                service.Handle(
                    new core.fs.Options.Import.Command(content, User.Identifier()),
                    token
                )
            );
        }

        [HttpGet]
        public Task<ActionResult> Dashboard([FromServices]core.fs.Options.Dashboard.Handler service)
            => this.OkOrError(
                service.Handle(
                    new core.fs.Options.Dashboard.Query(
                        User.Identifier()
                    )
                ));
    }
}