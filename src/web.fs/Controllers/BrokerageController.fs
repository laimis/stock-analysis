namespace web.Controllers

open System.IO
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Brokerage
open web.Utils

[<ApiController>]
[<Route("api/[controller]")>]
[<Authorize>]
type BrokerageController(brokerageHandler: BrokerageHandler) =
    inherit ControllerBase()

    [<HttpPost("buy")>]
    member this.Buy([<FromBody>] data: BuyOrSellData) =
        this.OkOrError(
            brokerageHandler.Handle(
                BrokerageTransaction.Buy(data, this.User.Identifier())
            )
        )

    [<HttpPost("buytocover")>]
    member this.BuyToCover([<FromBody>] data: BuyOrSellData) =
        this.OkOrError(
            brokerageHandler.Handle(
                BrokerageTransaction.BuyToCover(data, this.User.Identifier())
            )
        )

    [<HttpPost("sellshort")>]
    member this.SellShort([<FromBody>] data: BuyOrSellData) =
        this.OkOrError(
            brokerageHandler.Handle(
                BrokerageTransaction.SellShort(data, this.User.Identifier())
            )
        )

    [<HttpPost("sell")>]
    member this.Sell([<FromBody>] data: BuyOrSellData) =
        this.OkOrError(
            brokerageHandler.Handle(
                BrokerageTransaction.Sell(data, this.User.Identifier())
            )
        )

    [<HttpPost("optionsorder")>]
    member this.OptionsOrder() = task {
        use reader = new StreamReader(this.Request.Body, Encoding.UTF8)
        let! json = reader.ReadToEndAsync()
        let result = brokerageHandler.Handle({OptionOrderCommand.UserId = this.User.Identifier(); Payload = json})
        return! this.OkOrError(result)
    }

    [<HttpDelete("orders/{orderId}")>]
    member this.Delete([<FromRoute>] orderId: string) =
        this.OkOrError(
            brokerageHandler.Handle({CancelOrder.UserId = this.User.Identifier(); OrderId = orderId})
        )

    [<HttpGet("account")>]
    member this.GetAccount() =
        this.OkOrError(
            brokerageHandler.Handle({QueryAccount.UserId = this.User.Identifier()})
        )

    [<HttpGet("transactions")>]
    member this.GetTransactions() =
        this.OkOrError(
            brokerageHandler.Handle({QueryTransactions.UserId = this.User.Identifier()})
        )
