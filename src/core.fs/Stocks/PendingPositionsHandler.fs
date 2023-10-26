namespace core.fs.Stocks.PendingPositions

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs.Services
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.CSV
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
    }
        
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
    
    member this.HandleCreate userId (command:Create) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None ->
            return "User not found" |> ResponseUtils.failed
        | Some user ->
            
            let! existing = portfolio.GetPendingStockPositions userId
            
            let found = existing |> Seq.tryFind (fun x -> x.State.Ticker = command.Ticker && x.State.IsClosed = false)
            
            match found with
            | Some _ ->
                return "Position already exists" |> ResponseUtils.failed
            | None ->
                
                let orderType = Limit
                let duration = GtcPlus
                
                let! order = brokerage.BuyOrder user.State command.Ticker command.NumberOfShares command.Price orderType duration
                
                match order.Success with
                | None -> return order |> ResponseUtils.toOkOrError
                | Some _ ->
                    
                    let position = PendingStockPosition(
                        notes=command.Notes,
                        numberOfShares=command.NumberOfShares,
                        price=command.Price,
                        stopPrice=command.StopPrice,
                        strategy=command.Strategy,
                        ticker=command.Ticker,
                        userId=(userId |> IdentifierHelper.getUserId)
                    )
                    
                    do! portfolio.SavePendingPosition position userId
                    return Ok
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
                
                match account.Success with
                | None -> return account |> ResponseUtils.toOkOrError
                | Some account ->
                    
                    let! _ =
                        account.Orders
                        |> Seq.filter (fun x -> x.Ticker.Value = position.State.Ticker)
                        |> Seq.map (fun x -> brokerage.CancelOrder user.State x.OrderId |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    position.Close()
                
                    do! portfolio.SavePendingPosition position command.UserId
                
                    return Ok
    }
    
    member this.Handle (command:Export) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
        | Some _ ->
            
            let! positions = portfolio.GetPendingStockPositions(command.UserId)
            
            let data = positions |> Seq.sortByDescending(fun x -> x.State.Date);
                
            let filename = CSVExport.generateFilename("pendingpositions");

            let response = ExportResponse(filename, CSVExport.pendingPositions csvWriter data);
            
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
            
            let tickers = data |> Seq.map(fun x -> x.Ticker)
            let! priceResponse = brokerage.GetQuotes user.State tickers
            let prices =
                match priceResponse.Success with
                | None -> Dictionary<Ticker, StockQuote>()
                | Some prices -> prices
                
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
    
    