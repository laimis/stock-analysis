using System;
using System.IO;
using System.Threading.Tasks;
using core.fs.Notes;
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
        [HttpPost]
        public Task<ActionResult> Add([FromBody] AddNote input, [FromServices] Handler service) =>
            this.OkOrError(service.Handle(
                    new AddNote(note: input.Note, ticker: input.Ticker, userId: User.Identifier())
                )
            );

        [HttpPatch]
        public Task<ActionResult> Update([FromBody] UpdateNote input, [FromServices] Handler service) =>
            this.OkOrError(service.Handle(
                    new UpdateNote(
                        noteId: input.NoteId,
                        note: input.Note,
                        userId: User.Identifier()
                    )
                )
            );

        [HttpGet]
        public Task<ActionResult> List([FromQuery]string ticker, [FromServices] Handler service)
        {
            var response =
                ticker switch {
                    null => service.Handle(new GetNotes(User.Identifier())),
                    _ => service.Handle(new GetNotesForTicker(new core.Shared.Ticker(ticker), User.Identifier()))
                };

            return this.OkOrError(response);
        }

        [HttpGet("export")]
        public Task<ActionResult> Export([FromServices] Handler service) =>
            this.GenerateExport(service.Handle(new Export(User.Identifier())));

        [HttpGet("{noteId}")]
        public Task<ActionResult> Get([FromRoute] Guid noteId, [FromServices] Handler service) =>
            this.OkOrError(
                service.Handle(
                    new GetNote(userId: User.Identifier(), noteId: noteId)
                )
            );

        
        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file, [FromServices] Handler service)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var import = service.Handle(
                new Import(userId: User.Identifier(), content: content)
            );
            
            return await this.OkOrError(import);
        }
    }
}