using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using core.fs.Portfolio;
using web.Utils;

#nullable enable

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SECController(SECHandler handler) : ControllerBase
{
    [HttpGet("search")]
    public Task<ActionResult> Search([FromQuery] string query) =>
        this.OkOrError(handler.Handle(new SearchCompanies(query)));
    
    [HttpGet("filings/{ticker}")]
    public Task<ActionResult> GetFilings([FromRoute] string ticker) =>
        this.OkOrError(handler.Handle(new GetFilingsForTicker(ticker)));
    
    [HttpGet("portfolio-filings")]
    public Task<ActionResult> GetPortfolioFilings() =>
        this.OkOrError(handler.Handle(new GetPortfolioFilings(User.Identifier())));
}
