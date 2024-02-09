using System;
using System.IO;
using System.Threading.Tasks;
using core.fs.Cryptos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CryptosController(Handler service) : ControllerBase
    {
        [HttpGet()]
        public Task<ActionResult> Dashboard() =>
            this.OkOrError(service.Handle(new DashboardQuery(User.Identifier())));

        [HttpGet("{token}")]
        public Task<ActionResult> DetailsAsync([FromRoute] string token) =>
            this.OkOrError(service.Handle(new Details(token)));

        [HttpGet("{token}/ownership")]
        public Task<ActionResult> Ownership([FromRoute]string token) =>
            this.OkOrError(service.Handle(new OwnershipQuery(token, User.Identifier())));

        [HttpDelete("{token}/transactions/{transactionId}")]
        public Task<ActionResult> DeleteTransaction([FromRoute]string token, [FromRoute]Guid transactionId) =>
            this.OkOrError(service.Handle(new DeleteTransaction(token: token, transactionId: transactionId, userId:User.Identifier())));

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();
            
            var cmd = ImportCryptoCommandFactory.create(file.FileName, content, User.Identifier());

            return await this.OkOrError(service.Handle(cmd));
        }

        [HttpGet("export")]
        public Task<ActionResult> Export() =>
            this.GenerateExport(service.Handle(new Export(User.Identifier())));
    }
}
