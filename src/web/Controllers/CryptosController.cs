using System.IO;
using System.Threading.Tasks;
using core.Cryptos.Handlers;
using core.Shared;
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
    public class CryptosController : ControllerBase
    {
        private IMediator _mediator;

        public CryptosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet()]
        public async Task<object> Dashboard()
        {
            return await _mediator.Send(new Dashboard.Query(User.Identifier()));
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();
            
            var cmd = SelectCommand(file, content);

            cmd.WithUserId(this.User.Identifier());

            return this.OkOrError(await _mediator.Send(cmd));
        }

        private static RequestWithUserId<CommandResponse> SelectCommand(IFormFile file, string content) => file.FileName switch {
            string filename when filename.Contains("coinbasepro") => new ImportCoinbasePro.Command(content),
            string filename when filename.Contains("blockfi") => new ImportBlockFi.Command(content),
            string filename when filename.Contains("coinbase") => new ImportCoinbase.Command(content),
            _ => new ImportCoinbase.Command(content)
        };
    }
}
