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

        [HttpGet("{ticker}/details")]
        public async Task<ActionResult<OptionDetailsViewModel>> DetailsAsync(string ticker)
        {
            var details = await _mediator.Send(new Detail.Query(ticker));
            if (details == null)
            {
                return NotFound();
            }
            
            return details;
        }

        [HttpPost("open")]
        public async Task<ActionResult> Open(Buy.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<object> Get(Guid id)
        {
            var query = new Get.Query { Id = id };

            query.WithUserId(this.User.Identifier());
            
            var option =  await _mediator.Send(query);
            if (option == null)
            {
                return NotFound();
            }

            return option;
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }

        [HttpPost("import")]
        public async Task Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);
        }
    }
}