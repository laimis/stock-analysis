using System;
using System.IO;
using System.Threading.Tasks;
using core.Cryptos.Handlers;
using core.Cryptos.Views;
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
        public Task<CryptoDashboardView> Dashboard() =>
            _mediator.Send(new Dashboard.Query(User.Identifier()));

        [HttpGet("{token}")]
        public Task<CryptoDetailsView> DetailsAsync(string token) =>
            _mediator.Send(new Details.Query(token));

        [HttpGet("{token}/ownership")]
        public Task<CryptoOwnershipView> Ownership(string token) =>
            _mediator.Send(new Ownership.Query(token, User.Identifier()));

        [HttpDelete("{token}/transactions/{transactionId}")]
        public Task<bool> DeleteTransaction(string token, Guid transactionId) =>
            _mediator.Send(new DeleteTransaction.Command(token, transactionId, User.Identifier()));

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();
            
            var cmd = ImportCryptoCommandFactory.Create(file.FileName, content);

            cmd.WithUserId(User.Identifier());

            return this.OkOrError(await _mediator.Send(cmd));
        }
    }
}
