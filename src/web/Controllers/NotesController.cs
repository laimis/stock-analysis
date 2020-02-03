using System;
using System.IO;
using System.Threading.Tasks;
using core.Notes;
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
    public class NotesController : ControllerBase
    {
        private IMediator _mediator;

        public NotesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost]
        public async Task<object> Add(Add.Command input)
        {
            input.WithUserId(this.User.Identifier());

            var r = await _mediator.Send(input);

            if (r.Error != null)
            {
                return this.Error(r.Error);
            }

            return r.Aggregate;
        }

        [HttpPatch]
        public async Task<object> Update(Save.Command input)
        {
            input.WithUserId(this.User.Identifier());

            await _mediator.Send(input);
            
            return Ok();
        }

        [HttpGet]
        public async Task<object> List(string ticker)
        {
            var query = new List.Query(this.User.Identifier(), ticker);

            var result = await _mediator.Send(query);
            
            return result;
        }

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }

        [HttpGet("{noteId}")]
        public async Task<object> Get(Guid noteId)
        {
            var query = new Get.Query(this.User.Identifier(), noteId);

            var result = await _mediator.Send(query);
            
            return result;
        }
        
        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            cmd.WithUserId(this.User.Identifier());

            return this.OkOrError(await _mediator.Send(cmd));
        }
    }
}