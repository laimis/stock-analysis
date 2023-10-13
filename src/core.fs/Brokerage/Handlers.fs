module core.fs.Brokerage

    open System
    open System.ComponentModel.DataAnnotations
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.fs.Shared
    open core.fs.Shared.Adapters.Storage
    open core.fs.Shared.Domain.Accounts

    type BuyOrSellData =
        {
            [<Range(1, 1000)>]
            NumberOfShares:decimal
            [<Range(0, 2000)>]
            Price:decimal
            [<Required>]
            Type:BrokerageOrderType
            [<Required>]
            Duration:BrokerageOrderDuration
            [<Required>]
            Ticker:Ticker
        }
        
    type BrokerageTransaction =
        | Buy of BuyOrSellData * UserId
        | Sell of BuyOrSellData * UserId
        
    type CancelOrder =
        {
            UserId:UserId
            OrderId:string
        }
        
    type QueryAccount =
        {
            UserId:UserId
        }
        
    type Handler(accounts:IAccountStorage, brokerage:IBrokerage) =
        
        let buy (data:BuyOrSellData) user = 
            brokerage.BuyOrder(user, data.Ticker, data.NumberOfShares, data.Price, data.Type, data.Duration)
        let sell (data:BuyOrSellData) user = 
            brokerage.SellOrder(user, data.Ticker, data.NumberOfShares, data.Price, data.Type, data.Duration)
        
        interface IApplicationService
        
        member _.Handle (command:BrokerageTransaction) = task {
            
            let userId, data, func = 
                match command with
                | Buy (data,userId) -> (userId, data, buy)
                | Sell (data,userId) -> (userId, data, sell)
                
            let! user = accounts.GetUser(userId)
            
            match user with
            | None ->
                return "User not found" |> ResponseUtils.failed 
            | Some user ->
                let! _ = user.State |> func data
                return Ok
        }
        
        member _.Handle (command:CancelOrder) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | None -> return ResponseUtils.failed "User not found"
            | Some user ->
                let! _ = brokerage.CancelOrder(user.State, command.OrderId)
                return Ok
        }
        
        member _.Handle (query:QueryAccount) = task {
            let! user = accounts.GetUser(query.UserId)
            
            match user with
            | None -> return ResponseUtils.failedTyped<TradingAccount> "User not found"
            | Some user -> return! brokerage.GetAccount(user.State)
        }
            
        
        

