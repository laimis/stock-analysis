using System.Threading.Tasks;
using core.fs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BrokerageController : ControllerBase
    {
        [HttpPost("buy")]
        public Task<ActionResult> Buy(
            [FromBody] Brokerage.BuyOrSellData data,
            [FromServices] Brokerage.Handler handler) =>
            this.OkOrError(
                handler.Handle(
                    Brokerage.BrokerageTransaction.NewBuy(
                        data,
                        User.Identifier()
                    )
                )
            );
        
        [HttpPost("buytocover")]
        public Task<ActionResult> BuyToCover(
            [FromBody] Brokerage.BuyOrSellData data,
            [FromServices] Brokerage.Handler handler) =>
            this.OkOrError(
                handler.Handle(
                    Brokerage.BrokerageTransaction.NewBuyToCover(
                        data,
                        User.Identifier()
                    )
                )
            );
        
        [HttpPost("sellshort")]
        public Task<ActionResult> SellShort(
            [FromBody] Brokerage.BuyOrSellData data,
            [FromServices] Brokerage.Handler handler) =>
            this.OkOrError(
                handler.Handle(
                    Brokerage.BrokerageTransaction.NewSellShort(
                        data,
                        User.Identifier()
                    )
                )
            );

        [HttpPost("sell")]
        public Task<ActionResult> Sell(
            [FromBody] Brokerage.BuyOrSellData data,
            [FromServices] Brokerage.Handler handler) =>
            this.OkOrError(
                handler.Handle(
                    Brokerage.BrokerageTransaction.NewSell(
                        data,
                        User.Identifier()
                    )
                )
            );

        [HttpDelete("orders/{orderId}")]
        public Task<ActionResult> Delete([FromRoute] string orderId, [FromServices] Brokerage.Handler service) =>
            this.OkOrError(service.Handle(new Brokerage.CancelOrder(User.Identifier(), orderId)));

        [HttpGet("account")]
        public Task<ActionResult> GetAccount([FromServices] Brokerage.Handler service) =>
            this.OkOrError(service.Handle(new Brokerage.QueryAccount(User.Identifier())));
    }
}