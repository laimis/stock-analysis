namespace core.fs.Portfolio.PendingPositions

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Portfolio
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.CSV
open core.fs.Shared
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts

[<CLIMutable>]
[<Struct>]
type Create =
    {
        [<Required>]
        Notes: string
        [<Required>]
        [<Range(1, Int32.MaxValue)>]
        NumberOfShares: decimal
        [<Required>]
        [<Range(0.01, 100000.0)>]
        Price: decimal
        [<Range(0, 100000)>]
        StopPrice: Nullable<decimal>
        [<Required>]
        Ticker: Ticker
        [<Required(AllowEmptyStrings = false)>]
        Strategy: string
        UserId: UserId
    }
    
    static member WithUserId (userId:UserId) (command:Create) =
        { command with UserId = userId }
        
type Close =
    {
        [<Required>]
        PositionId: Guid
        UserId: UserId
    }
    
type Export =
    {
        UserId: UserId
    }
    
type Query =
    {
        UserId: UserId
    }

type Handler(accounts:IAccountStorage,brokerage:IBrokerage,portfolio:IPortfolioStorage,csvWriter:ICSVWriter) =
    
    interface IApplicationService
    
    member this.Handle (command:Create) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | Some user ->
            
            let! existing = portfolio.GetPendingStockPositions(command.UserId)
            
            let found = existing |> Seq.tryFind (fun x -> x.State.Ticker = command.Ticker && x.State.IsClosed = false)
            
            match found with
            | Some _ -> return "Position already exists" |> ResponseUtils.failed
            | None ->
                
                let! order = brokerage.BuyOrder(
                    user= user.State,
                    ticker= command.Ticker,
                    numberOfShares= command.NumberOfShares,
                    price= command.Price,
                    ``type``= BrokerageOrderType.Limit,
                    duration = BrokerageOrderDuration.GtcPlus
                )
                
                match order.IsOk with
                | false -> return order.Error.Message |> ResponseUtils.failed
                | true ->
                    
                    let position = PendingStockPosition(
                        notes=command.Notes,
                        numberOfShares=command.NumberOfShares,
                        price=command.Price,
                        stopPrice=command.StopPrice,
                        strategy=command.Strategy,
                        ticker=command.Ticker,
                        userId=(command.UserId |> IdentifierHelper.getUserId)
                    )
                    
                    do! portfolio.SavePendingPosition position command.UserId
                
                    return ServiceResponse()
    }
    
    member this.Handle (command:Close) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | Some user ->
            
            let! existing = portfolio.GetPendingStockPositions(command.UserId)
            
            let found = existing |> Seq.tryFind (fun x -> x.State.Id = command.PositionId)
            
            match found with
            | None -> return "Position not found" |> ResponseUtils.failed
            | Some position ->
                
                let! account = brokerage.GetAccount(user.State)
                
                match account.IsOk with
                | false -> return account.Error.Message |> ResponseUtils.failed
                | true ->
                    
                    let! _ =
                        account.Success.Orders
                        |> Seq.filter (fun x -> x.Ticker = position.State.Ticker)
                        |> Seq.map (fun x -> brokerage.CancelOrder(user.State, x.OrderId) |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    position.Close(purchased=false)
                
                    do! portfolio.SavePendingPosition position command.UserId
                
                    return ServiceResponse()
    }
    
    member this.Handle (command:Export) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
        | Some _ ->
            
            let! positions = portfolio.GetPendingStockPositions(command.UserId)
            
            let data = positions |> Seq.sortByDescending(fun x -> x.State.Date);
                
            let filename = CSVExport.GenerateFilename("pendingpositions");

            let response = ExportResponse(filename, CSVExport.Generate(csvWriter, data));
            
            return ServiceResponse<ExportResponse>(response)
    }
    
    member this.Handle (query:Query) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<PendingStockPositionState array>
        | Some user ->
            
            let! positions = portfolio.GetPendingStockPositions(query.UserId)
            
            let data =
                positions
                |> Seq.map(fun x -> x.State)
                |> Seq.filter (fun x -> x.IsClosed |> not)
                |> Seq.sortByDescending(fun x -> x.Date)
            
            let tickers = data |> Seq.map(fun x -> x.Ticker.Value)
            let! priceResponse = brokerage.GetQuotes(user.State, tickers)
            let prices =
                match priceResponse.IsOk with
                | false -> Dictionary<string, StockQuote>()
                | true -> priceResponse.Success
                
            let dataWithPrices =
                data
                |> Seq.map (fun d ->
                    match prices.TryGetValue(d.Ticker) with
                    | false, _ -> d
                    | true, price -> d.SetPrice(price.Price)
                )
                |> Seq.toArray
            
            return ServiceResponse<PendingStockPositionState array>(dataWithPrices)
    }
    
    