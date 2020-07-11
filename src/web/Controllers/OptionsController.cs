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
        private IMediator _mediator;

        public OptionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{ticker}/chain")]
        public async Task<ActionResult<OptionDetailsViewModel>> DetailsAsync(string ticker)
        {
            var details = await _mediator.Send(new Chain.Query(ticker));
            if (details == null)
            {
                return NotFound();
            }
            
            return details;
        }

        [HttpGet("{ticker}/active")]
        public Task<OwnedOptionStatsContainer> List(string ticker)
        {
            return _mediator.Send(new Active.Query(ticker, this.User.Identifier()));
        }

        [HttpGet("{id}")]
        public async Task<object> Get(Guid id)
        {
            var query = new Details.Query { Id = id };

            query.WithUserId(this.User.Identifier());
            
            var option =  await _mediator.Send(query);
            if (option == null)
            {
                return NotFound();
            }

            return option;
        }

        [HttpPost("sell")]
        public Task<object> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            return ExecTransaction(cmd);
        }

        [HttpPost("buy")]
        public Task<object> Buy(Buy.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());
            
            return ExecTransaction(cmd);
        }

        [HttpDelete("{id}")]
        public async Task<object> Delete(Guid id)
        {
            var cmd = new Delete.Command(id, this.User.Identifier());
            
            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpPost("expire")]
        public async Task<object> Expire(Expire.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

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
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            var userId = this.User.Identifier();

            cmd.WithUserId(userId);

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpGet]
        public Task<OwnedOptionStatsContainer> Dashboard()
        {
            return _mediator.Send(
                new Dashboard.Query(this.User.Identifier())
            );
        }
    }
}