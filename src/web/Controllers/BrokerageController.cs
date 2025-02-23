using System.IO;
using System.Text;
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
    public class BrokerageController(BrokerageHandler brokerageHandler) : ControllerBase
    {
        [HttpPost("buy")]
        public Task<ActionResult> Buy([FromBody] BuyOrSellData data) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewBuy(data, User.Identifier())
                )
            );
        
        [HttpPost("buytocover")]
        public Task<ActionResult> BuyToCover([FromBody] BuyOrSellData data) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewBuyToCover(data, User.Identifier())
                )
            );
        
        [HttpPost("sellshort")]
        public Task<ActionResult> SellShort([FromBody] BuyOrSellData data) =>
            this.OkOrError(
                brokerageHandler.Handle(
                    BrokerageTransaction.NewSellShort(data,User.Identifier())
                )
            );

        [HttpPost("sell")]
        public Task<ActionResult> Sell([FromBody] BuyOrSellData data) =>
            this.OkOrError(
                brokerageHandler.Handle(BrokerageTransaction.NewSell(data, User.Identifier()))
            );

        [HttpPost("optionsorder")]
        public async Task<ActionResult> OptionsOrder()
        {
            var reader = new StreamReader(Request.Body, Encoding.UTF8);

            var json = await reader.ReadToEndAsync();

            var result = brokerageHandler.Handle(new OptionOrderCommand(User.Identifier(), json));
            
            return await this.OkOrError(result);
        }

        [HttpDelete("orders/{orderId}")]
        public Task<ActionResult> Delete([FromRoute] string orderId) =>
            this.OkOrError(
                brokerageHandler.Handle(new CancelOrder(User.Identifier(), orderId)));

        [HttpGet("account")]
        public Task<ActionResult> GetAccount() =>
            this.OkOrError(
                brokerageHandler.Handle(new QueryAccount(User.Identifier())));
        
        [HttpGet("transactions")]
        public Task<ActionResult> GetTransactions() =>
            this.OkOrError(
                brokerageHandler.Handle(new QueryTransactions(User.Identifier())));
    }
}
