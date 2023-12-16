namespace core.fs.Stocks

open System
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.SEC
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Services
open core.fs.Services.Analysis
        
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
            
type StocksHandler(accounts:IAccountStorage,brokerage:IBrokerage,secFilings:ISECFilings) =
    
    interface IApplicationService
    
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
    
    