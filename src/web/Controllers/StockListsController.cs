using System;
using System.Threading.Tasks;
using core.fs.Stocks.Lists;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/stocks/lists")]
public class StockListsController(Handler handler) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult> StockLists() =>
        this.OkOrError(handler.Handle(new GetLists(User.Identifier())));

    [HttpPost]
    public Task<ActionResult> CreateStockList([FromBody]Create command) =>
        this.OkOrError(handler.HandleCreate(User.Identifier(), command));

    [HttpDelete("{id}")]
    public Task<ActionResult> DeleteStockList([FromRoute]Guid id) =>
        this.OkOrError(handler.Handle(new Delete(id, User.Identifier())));

    [HttpPost("{id}")]
    public Task UpdateStockList([FromBody]Update command) =>
        this.OkOrError(handler.HandleUpdate(command, User.Identifier()));

    [HttpPut("{id}")]
    public Task<ActionResult> AddStockToList([FromBody] AddStockToList command,
        [FromServices] Handler service) =>
        this.OkOrError(handler.HandleAddStockToList(User.Identifier(), command));

    [HttpDelete("{id}/{ticker}")]
    public Task<ActionResult> RemoveStockFromList([FromRoute] Guid id, [FromRoute] string ticker,
        [FromServices] Handler service) =>
        this.OkOrError(handler.Handle(new RemoveStockFromList(id, User.Identifier(), new Ticker(ticker))));

    [HttpPut("{id}/tags")]
    public Task<ActionResult> AddTagToStockList([FromBody]AddTagToList command) =>
        this.OkOrError(handler.HandleAddTagToList(User.Identifier(), command));

    [HttpDelete("{id}/tags/{tag}")]
    public Task<ActionResult> RemoveTagFromStockList([FromRoute] Guid id, [FromRoute] string tag) =>
        this.OkOrError(handler.Handle(new RemoveTagFromList(tag, id, User.Identifier())));

    [HttpGet("{id}")]
    public Task<ActionResult> GetStockList([FromRoute] Guid id) =>
        this.OkOrError(handler.Handle(new GetList(id, User.Identifier())));

    [HttpGet("{id}/export")]
    public Task<ActionResult> ExportStockList(Guid id, [FromQuery] bool justTickers) =>
        this.GenerateExport(
            handler.Handle(
                new ExportList(
                    justTickers: justTickers,
                    id: id,
                    userId: User.Identifier()
                )
            )
        );

    [HttpPost("{id}/clear")]
    public Task<ActionResult> ClearStockList([FromRoute] Guid id) =>
        this.OkOrError(handler.Handle(new Clear(id, User.Identifier())));
}
