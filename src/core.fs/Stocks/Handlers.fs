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
open core.fs.Shared.Domain
open core.fs.Shared.Domain.Accounts
    
type DashboardQuery =
    {
        UserId: UserId
    }
    
type DashboardView =
    {
        Positions: StockPositionWithCalculations seq
        Violations: StockViolationView seq
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
    
type OwnershipQuery =
    {
        Ticker: Ticker
        UserId: UserId
    }
    
type OwnershipView =
    {
        Ticker:Ticker
        CurrentPosition: StockPositionWithCalculations option
        Positions: StockPositionWithCalculations seq
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
            
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,secFilings:ISECFilings,portfolio:IPortfolioStorage,csvParser:ICSVParser,csvWriter:ICSVWriter) =
    
    interface IApplicationService
    
    
    member _.Handle (query:DashboardQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<DashboardView>
        | Some user ->
            let! stocks = portfolio.GetStockPositions query.UserId
            
            let positions =
                stocks
                |> Seq.filter _.IsOpen
                |> Seq.sortBy _.Ticker.Value
                |> Seq.map StockPositionWithCalculations
                |> Seq.toArray
                
            let! brokerageAccount = brokerage.GetAccount(user.State)
            let brokeragePositions =
                match brokerageAccount.Success with
                | Some account -> account.StockPositions
                | None -> Array.Empty<StockPosition>()
                
            let! quotesResponse =
                brokerage.GetQuotes
                    user.State
                    (brokeragePositions |> Seq.map _.Ticker |> Seq.append (positions |> Seq.map _.Ticker) |> Seq.distinct)
            
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
            let! stocks = portfolio.GetStockPositions query.UserId
            
            let trades =
                match query.ExportType with
                | ExportType.Open -> stocks |> Seq.filter (fun x -> x.IsOpen)
                | ExportType.Closed -> stocks |> Seq.filter (fun x -> x.IsClosed)
                | _ -> failwith "Unknown export type"
                
            let sorted =
                trades
                |> Seq.map StockPositionWithCalculations
                |> Seq.sortBy (fun x -> if x.Closed.IsSome then x.Closed.Value else x.Opened)
                
            let filename = CSVExport.generateFilename("positions")
            
            let response = ExportResponse(filename, CSVExport.trades csvWriter sorted)
            
            return ServiceResponse<ExportResponse>(response)
    }
    
    member _.Handle (query:ExportTransactions) = task {
        
        let! stocks = portfolio.GetStockPositions query.UserId
        
        let filename = CSVExport.generateFilename("stocks")
        
        let response = ExportResponse(filename, CSVExport.stocks csvWriter stocks)
        
        return ServiceResponse<ExportResponse>(response)
    }
    
    member _.Handle (query:OwnershipQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<OwnershipView>
        | Some user ->
            let! positions = portfolio.GetStockPositions query.UserId
            let positions = positions |> Seq.filter (fun x -> x.Ticker = query.Ticker) |> Seq.map StockPositionWithCalculations |> Seq.toList
            
            match positions with
            | [] -> return "Stock not found" |> ResponseUtils.failedTyped<OwnershipView>
            | _ ->
                let! priceResponse = brokerage.GetQuote user.State query.Ticker
                let price =
                    match priceResponse.Success with
                    | Some price -> Some price.Price
                    | None -> None
                    
                let openPosition = positions |> Seq.tryFind _.IsOpen
                    
                let view =
                    match openPosition with
                    | None -> {CurrentPosition=None; Ticker=query.Ticker; Positions=positions; Price=price}
                    | Some _ -> {CurrentPosition=openPosition; Ticker=query.Ticker; Positions=positions; Price=price}
                    
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
    
    