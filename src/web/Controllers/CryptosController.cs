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
    public class CryptosController : ControllerBase
    {
        [HttpGet()]
        public Task<ActionResult> Dashboard([FromServices] Handler service) =>
            this.OkOrError(service.Handle(new DashboardQuery(User.Identifier())));

        [HttpGet("{token}")]
        public Task<ActionResult> DetailsAsync([FromRoute] string token, [FromServices] Handler service) =>
            this.OkOrError(service.Handle(new Details(token)));

        [HttpGet("{token}/ownership")]
        public Task<ActionResult> Ownership([FromRoute]string token,[FromServices] Handler service) =>
            this.OkOrError(service.Handle(new OwnershipQuery(token, User.Identifier())));

        [HttpDelete("{token}/transactions/{transactionId}")]
        public Task<ActionResult> DeleteTransaction([FromRoute]string token, [FromRoute]Guid transactionId, [FromServices] Handler service) =>
            this.OkOrError(service.Handle(new DeleteTransaction(token: token, transactionId: transactionId, userId:User.Identifier())));

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file, [FromServices]Handler service)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();
            
            var cmd = ImportCryptoCommandFactory.create(file.FileName, content, User.Identifier());

            return await this.OkOrError(service.Handle(cmd));
        }

        [HttpGet("export")]
        public Task<ActionResult> Export([FromServices] Handler service) =>
            this.GenerateExport(service.Handle(new Export(User.Identifier())));
    }
}
