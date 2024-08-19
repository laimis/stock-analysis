namespace core.fs.Stocks.PendingPositions

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.CSV
open core.fs.Adapters.Storage
open core.fs.Services

[<CLIMutable>]
[<Struct>]
type Create =
    {
        [<Required>]
        Notes: string
        [<Required>]
        [<Range(-1000000, 1000000)>]
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
        [<Required>]
        UseLimitOrder: Boolean option
    }
        
type Close =
    {
        [<Required>]
        PositionId: Guid
        [<Required>]
        Reason: string
    }
    
type Export =
    {
        UserId: UserId
    }
    
type Query =
    {
        UserId: UserId
    }

type PendingStockPositionsHandler(accounts:IAccountStorage,brokerage:IBrokerage,portfolio:IPortfolioStorage,csvWriter:ICSVWriter) =
    
    interface IApplicationService
    
    member this.HandleCreate userId (command:Create) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None ->
            return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! existing = portfolio.GetPendingStockPositions userId
            
            let found = existing |> Seq.tryFind (fun x -> x.State.Ticker = command.Ticker && x.State.IsClosed = false)
            
            match found with
            | Some _ ->
                return "Position already exists" |> ServiceError |> Error
            | None ->
                
                // create position here so that validation runs and fails if something is wrong
                let position = PendingStockPosition(
                    notes=command.Notes,
                    numberOfShares=command.NumberOfShares,
                    price=command.Price,
                    stopPrice=command.StopPrice,
                    strategy=command.Strategy,
                    ticker=command.Ticker,
                    userId=(userId |> IdentifierHelper.getUserId)
                )
                
                let orderType =
                    match command.UseLimitOrder with
                    | Some useLimit ->
                        match useLimit with
                        | true -> Limit
                        | false -> Market
                    | None -> Market

                let duration =
                    match orderType with
                    | Limit -> GtcPlus
                    | _ -> Gtc
                    
                let orderTask =
                    match command.NumberOfShares with
                    | x when x > 0m -> brokerage.BuyOrder user.State command.Ticker command.NumberOfShares command.Price orderType duration
                    | x when x < 0m -> brokerage.SellShortOrder user.State command.Ticker (x |> abs) command.Price orderType duration
                    | _ -> failwith "Invalid number of shares"
                
                let! order = orderTask
                
                position.AddOrderDetails(orderType.ToString(), duration.ToString());
                
                match order with
                | Error err -> return Error err
                | Ok _ ->
                    do! portfolio.SavePendingPosition position userId
                    return Ok ()
    }
    
    member this.Handle (command:Close, userId:UserId) = task {
        let! user = accounts.GetUser(userId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! existing = portfolio.GetPendingStockPositions(userId)
            
            let found = existing |> Seq.tryFind (fun x -> x.State.Id = command.PositionId)
            
            match found with
            | None -> return "Position not found" |> ServiceError |> Error
            | Some position ->
                
                let! account = brokerage.GetAccount(user.State)
                
                match account with
                | Error err -> return Error err
                | Ok account ->
                    
                    let! _ =
                        account.Orders
                        |> Seq.filter (fun x -> x.Ticker = position.State.Ticker)
                        |> Seq.map (fun x -> brokerage.CancelOrder user.State x.OrderId |> Async.AwaitTask)
                        |> Async.Sequential
                        |> Async.StartAsTask
                        
                    position.Close(command.Reason);
                
                    do! portfolio.SavePendingPosition position userId
                
                    return Ok ()
    }
    
    member this.Handle (command:Export) = task {
        let! user = accounts.GetUser(command.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some _ ->
            
            let! positions = portfolio.GetPendingStockPositions(command.UserId)
            
            let data = positions |> Seq.sortByDescending _.State.Created
                
            let filename = CSVExport.generateFilename("pendingpositions")

            let response = ExportResponse(filename, CSVExport.pendingPositions CultureUtils.DefaultCulture csvWriter data)
            
            return Ok response
    }
    
    member this.Handle (query:Query) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! positions = portfolio.GetPendingStockPositions(query.UserId)
            
            let data =
                positions
                |> Seq.map _.State
                |> Seq.filter (fun x -> x.IsClosed |> not)
                |> Seq.sortByDescending _.Created
            
            let tickers = data |> Seq.map(_.Ticker)
            let! priceResponse = brokerage.GetQuotes user.State tickers
            let prices = priceResponse |> Result.defaultValue (Dictionary<Ticker, StockQuote>())
                
            let dataWithPrices =
                data
                |> Seq.map (fun d ->
                    match prices.TryGetValue(d.Ticker) with
                    | false, _ -> d
                    | true, price -> d.SetPrice(price.Price)
                )
                |> Seq.toArray
            
            return Ok dataWithPrices
    }
    
    
