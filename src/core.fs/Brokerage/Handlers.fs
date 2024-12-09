namespace core.fs.Brokerage

open System
open System.ComponentModel.DataAnnotations
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Storage
open core.fs.Stocks

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
        PositionId:System.Guid option
        Notes:string option
    }
    
type BrokerageTransaction =
    | Buy of BuyOrSellData * UserId
    | BuyToCover of BuyOrSellData * UserId
    | Sell of BuyOrSellData * UserId
    | SellShort of BuyOrSellData * UserId
    
type OptionOrderCommand = {
    UserId:UserId
    Payload:string
}
    
type CancelOrder =
    {
        UserId:UserId
        OrderId:string
    }
    
type QueryAccount =
    {
        UserId:UserId
    }
    
type QueryTransactions =
    {
        UserId:UserId
    }
    
type BrokerageHandler(accounts:IAccountStorage, brokerage:IBrokerage, portfolio:IPortfolioStorage) =
    
    let buy (data:BuyOrSellData) user = 
        brokerage.BuyOrder user data.Ticker data.NumberOfShares data.Price data.Type data.Duration
    let sell (data:BuyOrSellData) user = 
        brokerage.SellOrder user data.Ticker data.NumberOfShares data.Price data.Type data.Duration
    let buyToCover (data:BuyOrSellData) user = 
        brokerage.BuyToCoverOrder user data.Ticker data.NumberOfShares data.Price data.Type data.Duration
    let sellShort (data:BuyOrSellData) user =
        brokerage.SellShortOrder user data.Ticker data.NumberOfShares data.Price data.Type data.Duration
    
    interface IApplicationService
    
    member _.Handle (command:OptionOrderCommand) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! result = brokerage.OptionOrder user.State command.Payload
            return
                match result with
                | Error error -> Error error
                | Ok _ -> Ok ()
    }
    
    member _.Handle (command:BrokerageTransaction) = task {
        
        let userId, data, func = 
            match command with
            | Buy (data,userId) -> (userId, data, buy)
            | Sell (data,userId) -> (userId, data, sell)
            | BuyToCover (data,userId) -> (userId, data, buyToCover)
            | SellShort (data,userId) -> (userId, data, sellShort)
            
        let! user = accounts.GetUser(userId)
        
        match user with
        | None ->
            return "User not found" |> ServiceError |> Error 
        | Some user ->
            let! result = user.State |> func data
            match result with
            | Error error -> return Error error
            | Ok _ ->
                match data.PositionId, data.Notes with
                | Some positionId, Some _ -> 
                    let! positionOption = portfolio.GetStockPosition (positionId |> StockPositionId) userId
                    match positionOption with
                    | None -> return "Position not found" |> ServiceError |> Error
                    | Some position -> 
                        let positionWithNote = StockPosition.addNotes data.Notes DateTimeOffset.UtcNow position
                        do! portfolio.SaveStockPosition userId positionOption positionWithNote
                        return Ok ()
                | _ -> 
                    return Ok ()
    }
    
    member _.Handle (command:CancelOrder) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! _ = brokerage.CancelOrder user.State command.OrderId
            return Ok ()
    }
    
    member _.Handle (query:QueryAccount) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user -> return! brokerage.GetAccount(user.State)
    }
    
    member _.Handle (query:QueryTransactions) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            return!
                [|AccountTransactionType.Dividend; AccountTransactionType.Interest; AccountTransactionType.Fee; AccountTransactionType.Transfer|]
                |> brokerage.GetTransactions user.State 
    }
        
    
    

