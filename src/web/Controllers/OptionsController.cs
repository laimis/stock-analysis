using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Options;
using core.Shared.Adapters.Brokerage;
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
        public Task<object> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return ExecTransaction(cmd);
        }

        [HttpPost("buy")]
        public Task<object> Buy(Buy.Command cmd)
        {
            cmd.WithUserId(User.Identifier());
            
            return ExecTransaction(cmd);
        }

        [HttpDelete("{id}")]
        public async Task<object> Delete(Guid id)
        {
            var cmd = new core.Options.Delete.Command(id, User.Identifier());
            
            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpPost("expire")]
        public async Task<object> Expire(Expire.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            var r = await _mediator.Send(cmd);
            
            return this.OkOrError(r);
        }

        private async Task<object> ExecTransaction(OptionTransaction cmd)
        {
            var r = await _mediator.Send(cmd);

            if (r.Error != null)
            {
                return this.Error(r.Error);
            }

            return r.Aggregate;
        }


        [HttpGet("export")]
        public async Task<ActionResult> Export([FromServices]core.fs.Options.Export.Handler service)
        {
            var response = await service.Handle(
                new core.fs.Options.Export.Query(
                    User.Identifier()
                )
            );

            return this.GenerateExport(response);
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file, [FromServices]core.fs.Options.Import.Handler service, CancellationToken token)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync(token);

            var cmd = new core.fs.Options.Import.Command(content, User.Identifier());

            var r = service.Handle(cmd, token);

            return this.OkOrError(r);
        }

        [HttpGet]
        public Task<OptionDashboardView> Dashboard([FromServices]core.fs.Options.Dashboard.Handler service)
        {
            return service.Handle(
                new core.fs.Options.Dashboard.Query(
                    User.Identifier()
                )
            );
        }
    }
}