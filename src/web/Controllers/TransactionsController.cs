using System.IO;
using System.Threading.Tasks;
using core.fs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ImportTransactions.Handler _service;

        public TransactionsController(ImportTransactions.Handler service)
        {
            _service = service;
        }
        
        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new ImportTransactions.Command(content, User.Identifier());

            return await this.OkOrError(_service.Handle(cmd));
        }
    }
}