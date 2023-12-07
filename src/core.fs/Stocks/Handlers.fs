namespace core.fs.Stocks

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.CSV
open core.fs.Shared.Adapters.SEC
open core.fs.Shared.Adapters.Stocks
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
        Ticker: string
        Quote: StockQuote option
        Profile: StockProfile option
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
        Price:decimal option
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
        Frequency:PriceFrequency
    }
    
    static member NumberOfDays(frequency, numberOfDays, ticker, userId) =
        let totalDays = numberOfDays + 200 // to make sure we have enough for the moving averages
        let start = DateTimeOffset.UtcNow.AddDays(-totalDays)
        let ``end`` = DateTimeOffset.UtcNow
        
        {
            Ticker = ticker
            UserId = userId
            Start = start
            End = ``end``
            Frequency = frequency 
        }
    
type PricesView(prices:PriceBars) =
    member _.SMA = prices |> SMAContainer.Generate
    member _.PercentChanges = prices |> PercentChangeAnalysis.calculateForPriceBars
    member _.Prices = prices.Bars
    member _.ATR =
        let atrContainer = prices|> MultipleBarPriceAnalysis.Indicators.averageTrueRage

        let container = ChartDataPointContainer<decimal>($"ATR ({atrContainer.Period} days)", DataPointChartType.Line)
        
        atrContainer.DataPoints |> Array.iter container.Add
        
        container
        
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
    }
    
    
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
            let! stock = portfolio.GetStock data.Ticker userId
            
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
                let! updatedStock = portfolio.GetStock data.Ticker userId
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
            
            return Ok
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
                match brokerageAccount.Success with
                | Some account -> account.StockPositions
                | None -> Array.Empty<StockPosition>()
                
            let! quotesResponse =
                brokerage.GetQuotes
                    user.State
                    (brokeragePositions |> Seq.map (fun x -> x.Ticker) |> Seq.append (positions |> Seq.map (fun x -> x.Ticker)) |> Seq.distinct)
            
            let prices =
                match quotesResponse.Success with
                | Some prices -> prices
                | None -> Dictionary<Ticker,StockQuote>()
                
            let violations =
                match brokerageAccount.IsOk with
                | false -> Array.Empty<StockViolationView>()
                | true -> core.fs.Helpers.getViolations brokeragePositions positions prices |> Seq.toArray
                
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
            return Ok
    }
    
    member _.Handle (cmd:DeleteStop) = task {
        let! stock = portfolio.GetStock cmd.Ticker cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.DeleteStop()
            do! portfolio.Save stock cmd.UserId
            return Ok
    }
    
    member _.Handle (cmd:DeleteTransaction) = task {
        let! stock = portfolio.GetStock cmd.Ticker cmd.UserId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.DeleteTransaction(cmd.TransactionId)
            do! portfolio.Save stock cmd.UserId
            return Ok
    }
    
    member _.Handle (query:DetailsQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<DetailsView>
        | Some user ->
            let! profileResponse = brokerage.GetStockProfile user.State query.Ticker
            let! quoteResponse = brokerage.GetQuote user.State query.Ticker
                
            let view = {
                Ticker = query.Ticker.Value
                Quote = quoteResponse.Success
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
                
            let filename = CSVExport.generateFilename("positions")
            
            let response = ExportResponse(filename, CSVExport.trades csvWriter sorted)
            
            return ServiceResponse<ExportResponse>(response)
    }
    
    member _.Handle (query:ExportTransactions) = task {
        
        let! stocks = portfolio.GetStocks(query.UserId)
        
        let filename = CSVExport.generateFilename("stocks")
        
        let response = ExportResponse(filename, CSVExport.stocks csvWriter stocks)
        
        return ServiceResponse<ExportResponse>(response)
    }
    
    member this.Handle (cmd:ImportStocks) = task {
        let records = csvParser.Parse<ImportRecord>(cmd.Content)
        match records.Success with
        | None -> return records |> ResponseUtils.toOkOrError
        | Some records ->
            let! results =
                records
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
                
            return results |> ResponseUtils.toOkOrConcatErrors
    }
    
    member _.Handle (query:OwnershipQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<OwnershipView>
        | Some user ->
            let! stock = portfolio.GetStock query.Ticker query.UserId
            match stock with
            | null -> return "Stock not found" |> ResponseUtils.failedTyped<OwnershipView>
            | _ ->
                let! priceResponse = brokerage.GetQuote user.State query.Ticker
                let price =
                    match priceResponse.Success with
                    | Some price -> Some price.Price
                    | None -> None
                    
                let positions = stock.State.GetAllPositions()
                
                let view =
                    match stock.State.OpenPosition with
                    | null -> {Id=stock.State.Id; CurrentPosition=null; Ticker=stock.State.Ticker; Positions=positions; Price=price}
                    | _ -> {Id=stock.State.Id; CurrentPosition=stock.State.OpenPosition; Ticker=stock.State.Ticker; Positions=positions; Price=price}
                    
                return ServiceResponse<OwnershipView>(view)
    }
    
    member _.Handle (query:PriceQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<decimal option>
        | Some user ->
            let! priceResponse = brokerage.GetQuote user.State query.Ticker
            let price =
                match priceResponse.Success with
                | Some price -> Some price.Price
                | None -> None
                
            return ServiceResponse<decimal option>(price)
    }
    
    member _.Handle (query:PricesQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<PricesView>
        | Some user ->
            let! priceResponse = brokerage.GetPriceHistory user.State query.Ticker query.Frequency query.Start query.End
            match priceResponse.Success with
            | None -> return priceResponse.Error.Value.Message |> ResponseUtils.failedTyped<PricesView>
            | Some prices -> return prices |> PricesView |> ResponseUtils.success
    }
    
    member _.Handle (query:QuoteQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockQuote>
        | Some user ->
            let! priceResponse = brokerage.GetQuote user.State query.Ticker
            match priceResponse.Success with
            | None -> return priceResponse.Error.Value.Message |> ResponseUtils.failedTyped<StockQuote>
            | Some _ -> return priceResponse
    }
    
    member _.Handle (query:SearchQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<SearchResult array>
        | Some user ->
            let! matches = brokerage.Search user.State query.Term 10
            
            match matches.Success with
            | None -> return matches.Error.Value.Message |> ResponseUtils.failedTyped<SearchResult array>
            | Some _ -> return matches
    }
    
    member _.Handle (query:CompanyFilingsQuery) = task {
        let! response = secFilings.GetFilings(query.Ticker)
        match response.Success with
        | None -> return response.Error.Value.Message |> ResponseUtils.failedTyped<CompanyFiling seq>
        | Some response -> return ServiceResponse<CompanyFiling seq>(response.Filings)
    }
    
    member _.HandleSetStop userId (cmd:SetStop) = task {
        let! stock = portfolio.GetStock cmd.Ticker userId
        match stock with
        | null -> return "Stock not found" |> ResponseUtils.failed
        | _ ->
            stock.SetStop(cmd.StopPrice.Value) |> ignore
            do! portfolio.Save stock userId
            return Ok
    }
    
    