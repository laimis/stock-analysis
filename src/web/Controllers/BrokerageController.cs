using System.Threading.Tasks;
using core.fs.Brokerage;
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
            [FromBody] BuyOrSellData data,
            [FromServices] BrokerageHandler brokerageHandler) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewBuy(
                        data,
                        User.Identifier()
                    )
                )
            );
        
        [HttpPost("buytocover")]
        public Task<ActionResult> BuyToCover(
            [FromBody] BuyOrSellData data,
            [FromServices] BrokerageHandler brokerageHandler) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewBuyToCover(
                        data,
                        User.Identifier()
                    )
                )
            );
        
        [HttpPost("sellshort")]
        public Task<ActionResult> SellShort(
            [FromBody] BuyOrSellData data,
            [FromServices] BrokerageHandler brokerageHandler) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewSellShort(
                        data,
                        User.Identifier()
                    )
                )
            );

        [HttpPost("sell")]
        public Task<ActionResult> Sell(
            [FromBody] BuyOrSellData data,
            [FromServices] BrokerageHandler brokerageHandler) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewSell(
                        data,
                        User.Identifier()
                    )
                )
            );

        [HttpDelete("orders/{orderId}")]
        public Task<ActionResult> Delete([FromRoute] string orderId, [FromServices] BrokerageHandler service) =>
            this.OkOrError(service.Handle(new CancelOrder(User.Identifier(), orderId)));

        [HttpGet("account")]
        public Task<ActionResult> GetAccount([FromServices] BrokerageHandler service) =>
            this.OkOrError(service.Handle(new QueryAccount(User.Identifier())));
    }
}