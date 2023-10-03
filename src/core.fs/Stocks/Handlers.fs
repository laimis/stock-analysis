namespace core.fs.Stocks

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.CSV
open core.Shared.Adapters.SEC
open core.Shared.Adapters.Stocks
open core.Stocks
open core.Stocks.Services
open core.Stocks.Services.Analysis
open core.fs.Shared
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts


type StockTransaction =
    {
        [<Range(1, 1000000)>]
        NumberOfShares: decimal
        [<Range(0, 100000)>]
        Price: decimal
        [<Required>]
        Date: Nullable<DateTimeOffset>
        StopPrice: Nullable<decimal>
        Notes: string
        BrokerageOrderId: string
        Strategy: string
        Ticker: Ticker
    }
    
type BuyOrSell =
    | Buy of StockTransaction * UserId
    | Sell of StockTransaction * UserId
    
type DashboardQuery =
    {
        UserId: UserId
    }
    
type DashboardView =
    {
        Positions: PositionInstance seq
        Violations: StockViolationView seq
    }

type DeleteStock =
    {
        StockId: Guid
        UserId: UserId
    }
    
type DeleteStop =
    {
        Ticker: Ticker
        UserId: UserId
    }
    
type DeleteTransaction =
    {
        Ticker: Ticker
        UserId: UserId
        TransactionId: Guid
    }
    
type DetailsQuery =
    {
        Ticker: Ticker
        UserId: UserId
    }
    
type DetailsView =
    {
        Ticker: Ticker
        Price: Nullable<decimal>
        Profile: StockProfile
    }
    
type ExportType =
    | Open = 0
    | Closed = 1
    
type ExportTrades =
    {
        UserId: UserId
        ExportType: ExportType
    }

type ExportTransactions =
    {
        UserId: UserId
    }
    
type ImportStocks =
    {
        UserId: UserId
        Content: string
    }

type private ImportRecord =
    {
         amount:decimal
         ``type``:string
         date:Nullable<DateTimeOffset>
         price:decimal
         ticker:string
    }
    
type OwnershipQuery =
    {
        Ticker: Ticker
        UserId: UserId
    }
    
type OwnershipView =
    {
        Id:Guid
        CurrentPosition: PositionInstance
        Ticker:Ticker
        Positions: PositionInstance seq
    }
    
type PriceQuery =
    {
        UserId:UserId
        Ticker:Ticker
    }
    
type PricesQuery =
    {
        UserId:UserId
        Ticker:Ticker
        Start:DateTimeOffset
        End:DateTimeOffset
    }
    
    static member NumberOfDays(numberOfDays, ticker, userId) =
        let totalDays = numberOfDays + 200 // to make sure we have enough for the moving averages
        let start = DateTimeOffset.UtcNow.AddDays(-totalDays)
        let ``end`` = DateTimeOffset.UtcNow
        
        {
            Ticker = ticker
            UserId = userId
            Start = start
            End = ``end``
        }
    
type PricesView(prices:PriceBar array) =
    member _.SMA = SMAContainer.Generate(prices)
    member _.PercentChanges = NumberAnalysis.PercentChanges(prices)
    member _.Prices = prices

type QuoteQuery =
    {
        Ticker:Ticker
        UserId:UserId
    }
    
type SearchQuery =
    {
        Term:string
        UserId:UserId
    }
    
type CompanyFilingsQuery =
    {
        Ticker:Ticker
        UserId:UserId
    }
    
type SetStop =
    {
        [<Required>]
        StopPrice:Nullable<decimal>
        Ticker:Ticker
        UserId:UserId
    }
    
    static member WithUserId userId (cmd:SetStop) =
        {cmd with UserId=userId}
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,secFilings:ISECFilings,portfolio:IPortfolioStorage,csvParser:ICSVParser,csvWriter:ICSVWriter) =
    
    interface IApplicationService
    
    member _.Handle(cmd:BuyOrSell) = task {
        
        let buyOrSellFunction (stock:OwnedStock) =
            match cmd with
            | Buy (data, _) -> stock.Purchase(numberOfShares=data.NumberOfShares, price=data.Price, date=data.Date.Value, stopPrice=data.StopPrice, notes=data.Notes)
            | Sell (data, _) -> stock.Sell(numberOfShares=data.NumberOfShares, price=data.Price, date=data.Date.Value, notes=data.Notes)
            
        let data, userId =
            match cmd with
            | Buy (data, userId) -> (data, userId)
            | Sell (data, userId) -> (data, userId)
            
        let! user = accounts.GetUser(userId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = portfolio.GetStock data.Ticker.Value userId
            
            let stockToUse =
                match stock with
                | null -> OwnedStock(ticker=data.Ticker, userId=(userId |> IdentifierHelper.getUserId))
                | _ -> stock
                
            let isNewPosition = stockToUse.State.OpenPosition = null
            
            stockToUse |> buyOrSellFunction
            
            match isNewPosition && data.Strategy <> null with
            | true -> stockToUse.SetPositionLabel(positionId=stockToUse.State.OpenPosition.PositionId, key="strategy", value=data.Strategy) |> ignore
            | _ -> ()
            
            do! portfolio.Save stockToUse userId
            
            let! pendingPositions = portfolio.GetPendingStockPositions(userId)
            let pendingPositionOption = pendingPositions |> Seq.tryFind (fun x -> x.State.Ticker = data.Ticker && x.State.IsClosed = false)
            
            match (pendingPositionOption, isNewPosition) with
            | Some pendingPosition, true ->
                // transfer some data from pending position to this new position
                let! updatedStock = portfolio.GetStock data.Ticker.Value userId
                let opened = updatedStock.State.OpenPosition
                
                let stopSet = updatedStock.SetStop(pendingPosition.State.StopPrice.Value)
                
                let notesSet =
                    match opened.Notes.Count with
                    | 0 -> updatedStock.AddNotes(pendingPosition.State.Notes)
                    | _ -> false
                
                let strategySet =
                    match pendingPosition.State.Strategy with
                    | null -> false
                    | _ -> updatedStock.SetPositionLabel(positionId=opened.PositionId, key="strategy", value=pendingPosition.State.Strategy)
                    
                match stopSet || notesSet || strategySet with
                | true -> do! portfolio.Save updatedStock userId
                | false -> ()
                
                pendingPosition.Purchase stockToUse.State.OpenPosition.AverageCostPerShare
                
                do! portfolio.SavePendingPosition pendingPosition userId
                
            | _ -> ()
            
            return ServiceResponse()
    }
    
    member _.Handle (query:DashboardQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<DashboardView>
        | Some user ->
            let! stocks = portfolio.GetStocks(query.UserId)
            
            let positions =
                stocks
                |> Seq.filter (fun stock -> stock.State.OpenPosition <> null)
                |> Seq.map (fun stock -> stock.State.OpenPosition)
                |> Seq.sortBy (fun position -> position.Ticker.Value)
                
            let! brokerageAccount = brokerage.GetAccount(user.State)
            let brokeragePositions =
                match brokerageAccount.IsOk with
                | false -> Array.Empty<StockPosition>()
                | true -> brokerageAccount.Success.StockPositions
                
            let! quotesResponse = brokerage.GetQuotes(
                user.State,
                brokeragePositions |> Seq.map (fun x -> x.Ticker.Value) |> Seq.append (positions |> Seq.map (fun x -> x.Ticker.Value)) |> Seq.distinct)
            
            let prices =
                match quotesResponse.IsOk with
                | false -> Dictionary<string,StockQuote>()
                | true -> quotesResponse.Success
                
            let violations =
                match brokerageAccount.IsOk with
                | false -> Array.Empty<StockViolationView>()
                | true -> Helpers.getViolations brokeragePositions positions prices |> Seq.toArray
                
            // TODO: how to eliminate this state manipulation
            positions |> Seq.iter( fun p ->
                let price =
                    match prices.TryGetValue(p.Ticker) with
                    | true, price -> price.Price
                    | false, _ -> 0m
                p.SetPrice(price)
            )
            
            let dashboard = {
                Positions = positions
                Violations = violations
            }
            
            return ServiceResponse<DashboardView>(dashboard)
    }

    member _.Handle (cmd:DeleteStock) = task {
        
        let! stock = portfolio.GetStockByStockId cmd.StockId cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.Delete()
            do! portfolio.Save stock cmd.UserId
            return ServiceResponse()
    }
    
    member _.Handle (cmd:DeleteStop) = task {
        let! stock = portfolio.GetStock cmd.Ticker.Value cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.DeleteStop()
            do! portfolio.Save stock cmd.UserId
            return ServiceResponse()
    }
    
    member _.Handle (cmd:DeleteTransaction) = task {
        let! stock = portfolio.GetStock cmd.Ticker.Value cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.DeleteTransaction(cmd.TransactionId)
            do! portfolio.Save stock cmd.UserId
            return ServiceResponse()
    }
    
    member _.Handle (query:DetailsQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<DetailsView>
        | Some user ->
            let! profileResponse = brokerage.GetStockProfile(user.State, query.Ticker)
            let! priceResponse = brokerage.GetQuote(user.State, query.Ticker)
            let price =
                match priceResponse.IsOk with
                | true -> Nullable<decimal>(priceResponse.Success.Price)
                | false -> Nullable<decimal>()
                
            let view = {
                Ticker = query.Ticker
                Price = price
                Profile = profileResponse.Success
            }
            
            return ServiceResponse<DetailsView>(view)
    }
    
    member _.Handle (query:ExportTrades) = task {
        
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<ExportResponse>
        | _ ->
            let! stocks = portfolio.GetStocks(query.UserId)
            
            let trades =
                match query.ExportType with
                | ExportType.Open -> stocks |> Seq.filter (fun x -> x.State.OpenPosition <> null) |> Seq.map (fun x -> x.State.OpenPosition)
                | ExportType.Closed -> stocks |> Seq.collect (fun x -> x.State.GetClosedPositions())
                | _ -> failwith "Unknown export type"
                
            let sorted =
                trades
                |> Seq.sortBy (fun x -> if x.Closed.HasValue then x.Closed.Value else x.Opened)
                
            let filename = CSVExport.GenerateFilename("positions")
            
            let response = ExportResponse(filename, CSVExport.Generate(csvWriter, sorted))
            
            return ServiceResponse<ExportResponse>(response)
    }
    
    member _.Handle (query:ExportTransactions) = task {
        
        let! stocks = portfolio.GetStocks(query.UserId)
        
        let filename = CSVExport.GenerateFilename("stocks")
        
        let response = ExportResponse(filename, CSVExport.Generate(csvWriter, stocks))
        
        return ServiceResponse<ExportResponse>(response)
    }
    
    member this.Handle (cmd:ImportStocks) = task {
        let records = csvParser.Parse<ImportRecord>(cmd.Content)
        match records.IsOk with
        | false -> return records.Error.Message |> ResponseUtils.failed
        | _ ->
            let! results =
                records.Success
                |> Seq.map (fun r -> async {
                    let command =
                        match r.``type`` with
                        | "buy" -> Buy({
                            NumberOfShares = r.amount
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = ""
                            BrokerageOrderId = ""
                            Strategy = ""
                            Ticker = Ticker(r.ticker)
                        }, cmd.UserId)
                        | "sell" -> Sell({
                            NumberOfShares = r.amount
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = ""
                            BrokerageOrderId = ""
                            Strategy = ""
                            Ticker = Ticker(r.ticker)
                        }, cmd.UserId)
                        | _ -> failwith "Unknown transaction type"
                        
                    let! result = this.Handle(command) |> Async.AwaitTask
                    return result
                })
                |> Async.Sequential
                |> Async.StartAsTask
                
            let failed =
                results |> Seq.filter (fun x -> x.IsOk = false) |> Seq.map (fun x -> x.Error.Message) |> Seq.toArray
                
            match failed.Length with
            | 0 -> return ServiceResponse()
            | _ -> return String.Concat(failed, ",") |> ResponseUtils.failed
    }
    
    member _.Handle (query:OwnershipQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<OwnershipView>
        | Some user ->
            let! stock = portfolio.GetStock query.Ticker.Value query.UserId
            match stock with
            | null -> return "Stock not found" |> ResponseUtils.failedTyped<OwnershipView>
            | _ ->
                let! priceResponse = brokerage.GetQuote(user.State, query.Ticker)
                let price =
                    match priceResponse.IsOk with
                    | true -> Nullable<decimal>(priceResponse.Success.Price)
                    | false -> Nullable<decimal>()
                    
                if stock.State.OpenPosition <> null then
                    stock.State.OpenPosition.SetPrice(price)
                else
                    ()
                    
                let positions = stock.State.GetAllPositions()
                
                let view =
                    match stock.State.OpenPosition with
                    | null -> {Id=stock.State.Id; CurrentPosition=null; Ticker=stock.State.Ticker; Positions=positions}
                    | _ -> {Id=stock.State.Id; CurrentPosition=stock.State.OpenPosition; Ticker=stock.State.Ticker; Positions=positions}
                    
                return ServiceResponse<OwnershipView>(view)
    }
    
    member _.Handle (query:PriceQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<Nullable<decimal>>
        | Some user ->
            let! priceResponse = brokerage.GetQuote(user.State, query.Ticker)
            let price =
                match priceResponse.IsOk with
                | true -> Nullable<decimal>(priceResponse.Success.Price)
                | false -> Nullable<decimal>()
                
            return ServiceResponse<Nullable<decimal>>(price)
    }
    
    member _.Handle (query:PricesQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<PricesView>
        | Some user ->
            let! priceResponse = brokerage.GetPriceHistory(user.State, query.Ticker, start=query.Start, ``end``=query.End)
            match priceResponse.IsOk with
            | false -> return priceResponse.Error.Message |> ResponseUtils.failedTyped<PricesView>
            | true ->
                let view = PricesView(priceResponse.Success)
                return ServiceResponse<PricesView>(view)
    }
    
    member _.Handle (query:QuoteQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockQuote>
        | Some user ->
            let! priceResponse = brokerage.GetQuote(user.State, query.Ticker)
            match priceResponse.IsOk with
            | false -> return priceResponse.Error.Message |> ResponseUtils.failedTyped<StockQuote>
            | true -> return ServiceResponse<StockQuote>(priceResponse.Success)
    }
    
    member _.Handle (query:SearchQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<SearchResult seq>
        | Some user ->
            let! matches = brokerage.Search(user.State, query.Term)
            
            match matches.IsOk with
            | false -> return matches.Error.Message |> ResponseUtils.failedTyped<SearchResult seq>
            | true -> return ServiceResponse<SearchResult seq>(matches.Success)
    }
    
    member _.Handle (query:CompanyFilingsQuery) = task {
        let! response = secFilings.GetFilings(query.Ticker)
        match response.IsOk with
        | false -> return response.Error.Message |> ResponseUtils.failedTyped<CompanyFiling seq>
        | true -> return ServiceResponse<CompanyFiling seq>(response.Success.filings)
    }
    
    member _.Handle (cmd:SetStop) = task {
        let! stock = portfolio.GetStock cmd.Ticker.Value cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.SetStop(cmd.StopPrice.Value) |> ignore
            do! portfolio.Save stock cmd.UserId
            return ServiceResponse()
    }
    
    