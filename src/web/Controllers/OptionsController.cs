using System;
using System.IO;
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

        public OptionsController(IMediator mediator)
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
        public Task<OwnedOptionStatsView> List(string ticker) =>
            _mediator.Send(new List.Query(ticker, User.Identifier()));

        [HttpGet("{id}")]
        public Task<OwnedOptionView> Get(Guid id) =>
            _mediator.Send(new Details.Query(id, User.Identifier()));

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
            var cmd = new Delete.Command(id, User.Identifier());
            
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
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(User.Identifier()));
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            var userId = User.Identifier();

            cmd.WithUserId(userId);

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpGet]
        public Task<OptionDashboardView> Dashboard()
        {
            var query = new Dashboard.Query(User.Identifier());
            
            return _mediator.Send(query);
        }
    }
}