using System;
using System.IO;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpPost("importshares")]
        public async Task ImportShares(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            while(true)
            {
                var line = await streamReader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                Console.WriteLine("processing " + line);
                var parts = line.Split(',');

                var ticker = parts[0];
                if (ticker != "ticker")
                {
                    await ProcessLine(parts, ticker);
                }
            }
        }

        private async Task ProcessLine(string[] parts, string ticker)
        {
            var type = parts[1];
            var amount = Int32.Parse(parts[2]);
            var price = double.Parse(parts[3]);
            var date = DateTimeOffset.Parse(parts[4]);

            object cmd = null;
            switch (type)
            {
                case "buy":
                    var b = new core.Stocks.Buy.Command
                    {
                        Amount = amount,
                        Date = date,
                        Price = price,
                        Ticker = ticker,
                    };

                    b.WithUserId(this.User.Identifier());
                    cmd = b;
                    break;

                case "sell":
                    var s = new core.Stocks.Sell.Command
                    {
                        Amount = amount,
                        Date = date,
                        Price = price,
                        Ticker = ticker,
                    };

                    s.WithUserId(this.User.Identifier());
                    cmd = s;
                    break;
            }

            Console.WriteLine("Sending to mediator " + cmd);
            await _mediator.Send(cmd);
        }
    }
}