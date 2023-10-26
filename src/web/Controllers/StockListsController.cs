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
public class StockListsController : ControllerBase
{
    private readonly Handler _handler;
    
    public StockListsController(Handler handler) => _handler = handler;
    
    [HttpGet]
    public Task<ActionResult> StockLists() =>
        this.OkOrError(_handler.Handle(new GetLists(User.Identifier())));

    [HttpPost]
    public Task<ActionResult> CreateStockList([FromBody]Create command) =>
        this.OkOrError(_handler.HandleCreate(User.Identifier(), command));

    [HttpDelete("{name}")]
    public Task<ActionResult> DeleteStockList([FromRoute]string name) =>
        this.OkOrError(_handler.Handle(new Delete(name, User.Identifier())));

    [HttpPost("{name}")]
    public Task UpdateStockList([FromBody]Update command) =>
        this.OkOrError(_handler.HandleUpdate(User.Identifier(), command));

    [HttpPut("{name}")]
    public Task<ActionResult> AddStockToList([FromBody] AddStockToList command,
        [FromServices] Handler service) =>
        this.OkOrError(_handler.HandleAddStockToList(User.Identifier(), command));

    [HttpDelete("{name}/{ticker}")]
    public Task<ActionResult> RemoveStockFromList([FromRoute] string name, [FromRoute] string ticker,
        [FromServices] Handler service) =>
        this.OkOrError(_handler.Handle(new RemoveStockFromList(name, User.Identifier(), new Ticker(ticker))));

    [HttpPut("{name}/tags")]
    public Task<ActionResult> AddTagToStockList([FromBody]AddTagToList command) =>
        this.OkOrError(_handler.HandleAddTagToList(User.Identifier(), command));

    [HttpDelete("{name}/tags/{tag}")]
    public Task<ActionResult> RemoveTagFromStockList([FromRoute] string name, [FromRoute] string tag) =>
        this.OkOrError(_handler.Handle(new RemoveTagFromList(tag, name, User.Identifier())));

    [HttpGet("{name}")]
    public Task<ActionResult> GetStockList([FromRoute] string name) =>
        this.OkOrError(_handler.Handle(new GetList(name, User.Identifier())));

    [HttpGet("{name}/export")]
    public Task<ActionResult> ExportStockList(string name, [FromQuery] bool justTickers) =>
        this.GenerateExport(
            _handler.Handle(
                new ExportList(
                    justTickers: justTickers,
                    name: name,
                    userId: User.Identifier()
                )
            )
        );

    
}