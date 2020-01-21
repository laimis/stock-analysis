using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.Notes;
using MediatR;
using Microsoft.AspNetCore.Authentication;
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
        public async Task<object> Add(AddNote.Command input)
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
    }
}