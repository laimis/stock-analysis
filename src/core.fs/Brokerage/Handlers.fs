module core.fs.Brokerage

    open System
    open System.ComponentModel.DataAnnotations
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.fs.Shared
    open core.fs.Shared.Adapters.Storage

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
        | Buy of BuyOrSellData * Guid
        | Sell of BuyOrSellData * Guid
        
    type CancelOrder =
        {
            UserId:Guid
            OrderId:string
        }
        
    type QueryAccount =
        {
            UserId:Guid
        }
        
    type Handler(accounts:IAccountStorage, brokerage:IBrokerage) =
        
        let buy (data:BuyOrSellData) user = 
            brokerage.BuyOrder(user, data.Ticker, data.NumberOfShares, data.Price, data.Type, data.Duration)
        let sell (data:BuyOrSellData) user = 
            brokerage.SellOrder(user, data.Ticker, data.NumberOfShares, data.Price, data.Type, data.Duration)
        
        interface IApplicationService
        
        member _.Handle (command:BrokerageTransaction) = task {
            
            let (userId, data, func) = 
                match command with
                | Buy (data,userId) -> (userId, data, buy)
                | Sell (data,userId) -> (userId, data, sell)
                
            let! user = accounts.GetUser(userId)
            
            match user with
            | null ->
                return ResponseUtils.failed "User not found"
            | _ ->
                let! _ = user.State |> func data
                return ServiceResponse()
        }
        
        member _.Handle (command:CancelOrder) = task {
            let! user = accounts.GetUser(command.UserId)
            
            match user with
            | null ->
                return ResponseUtils.failed "User not found"
            | _ ->
                let! _ = brokerage.CancelOrder(user.State, command.OrderId)
                return ServiceResponse()
        }
        
        member _.Handle (query:QueryAccount) = task {
            let! user = accounts.GetUser(query.UserId)
            
            match user with
            | null ->
                return ResponseUtils.failedTyped<TradingAccount> "User not found"
            | _ ->
                return! brokerage.GetAccount(user.State)
        }
            
        
        

