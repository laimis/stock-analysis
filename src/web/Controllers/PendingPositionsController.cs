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
public class PendingPositionsController : ControllerBase
{
    private readonly PendingStockPositionsHandler _pendingStockPositionsHandler;
    public PendingPositionsController(PendingStockPositionsHandler pendingStockPositionsHandler) => _pendingStockPositionsHandler = pendingStockPositionsHandler;
    
    [HttpGet]
    public Task<ActionResult> PendingStockPositions() =>
        this.OkOrError(_pendingStockPositionsHandler.Handle(new Query(User.Identifier())));
        
    [HttpGet("export")]
    public Task<ActionResult> ExportPendingStockPositions() =>
        this.GenerateExport(
            _pendingStockPositionsHandler.Handle(new Export(User.Identifier())
            ));
        
    [HttpPost]
    public Task<ActionResult> CreatePendingStockPosition([FromBody]Create command) =>
        this.OkOrError(
            _pendingStockPositionsHandler.HandleCreate(
                User.Identifier(), command
            )
        );

    [HttpDelete("{id}")]
    public Task<ActionResult> ClosePendingStockPosition([FromRoute] Guid id) =>
        this.OkOrError(_pendingStockPositionsHandler.Handle(new Close(id, User.Identifier())));
}