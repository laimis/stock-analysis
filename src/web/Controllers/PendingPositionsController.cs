using System;
using System.Threading.Tasks;
using core.fs.Stocks.PendingPositions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/stocks/pendingpositions")]
public class PendingPositionsController(PendingStockPositionsHandler pendingStockPositionsHandler) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> PendingStockPositions() =>
        this.OkOrError(pendingStockPositionsHandler.Handle(new Query(User.Identifier())));
        
    [HttpGet("export")]
    public Task<ActionResult> ExportPendingStockPositions() =>
        this.GenerateExport(
            pendingStockPositionsHandler.Handle(new Export(User.Identifier())
            ));
        
    [HttpPost]
    public Task<ActionResult> CreatePendingStockPosition([FromBody]Create command) =>
        this.OkOrError(
            pendingStockPositionsHandler.HandleCreate(
                User.Identifier(), command
            )
        );

    [HttpDelete("{id}")]
    public Task<ActionResult> ClosePendingStockPosition([FromRoute] Guid id) =>
        this.OkOrError(pendingStockPositionsHandler.Handle(new Close(id, User.Identifier())));
}
