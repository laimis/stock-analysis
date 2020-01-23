using System.Threading.Tasks;
using core.Notes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

            return await _mediator.Send(input);
        }

        [HttpPost("{id}/archive")]
        public async Task<ActionResult> Archive(string id)
        {
            var cmd = new Archive.Command {
                Id = id
            };

            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);
            
            return Ok();
        }

        [HttpPost("reminder")]
        public async Task<ActionResult> Reminder(Reminder.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);
            
            return Ok();
        }

        [HttpPatch]
        public async Task<object> Update(Save.Command input)
        {
            input.WithUserId(this.User.Identifier());

            await _mediator.Send(input);
            
            return Ok();
        }

        [HttpGet]
        public async Task<object> Get()
        {
            var query = new List.Query(this.User.Identifier());

            var result = await _mediator.Send(query);
            
            return result;
        }

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }

        [HttpGet("{noteId}")]
        public async Task<object> Get(string noteId)
        {
            var query = new Get.Query(this.User.Identifier(), noteId);

            var result = await _mediator.Send(query);
            
            return result;
        }
    }
}