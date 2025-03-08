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
        Start:DateTimeOffset option
        End:DateTimeOffset option
        Frequency:PriceFrequency
    }
    
    static member NumberOfDays(frequency, numberOfDays, ticker, userId) =
        let totalDays = numberOfDays + 200 // to make sure we have enough for the moving averages
        let start = DateTimeOffset.UtcNow.AddDays(-totalDays) |> Some
        let ``end`` = DateTimeOffset.UtcNow |> Some
        
        {
            Ticker = ticker
            UserId = userId
            Start = start
            End = ``end``
            Frequency = frequency 
        }
    
type PricesView(prices:PriceBars) =
    member _.MovingAverages = prices |> MovingAveragesContainer.Generate
    member _.PercentChanges = prices |> PercentChangeAnalysis.calculateForPriceBars true
    member _.Prices = prices.Bars
    member _.ATR =
        let atrContainer = prices|> MultipleBarPriceAnalysis.Indicators.averageTrueRage
        let container = ChartDataPointContainer<decimal>($"ATR ({atrContainer.Period} days)", DataPointChartType.Line)
        atrContainer.DataPoints |> Array.iter container.Add
        container
        
    member _.ATRPercent =
        let atrContainer = prices|> MultipleBarPriceAnalysis.Indicators.averageTrueRangePercentage
        let container = ChartDataPointContainer<decimal>($"ATR %% ({atrContainer.Period} days)", DataPointChartType.Line)
        atrContainer.DataPoints |> Array.iter container.Add
        container

    member _.OBV =
        let obvContainer = prices.Bars |> MultipleBarPriceAnalysis.Indicators.onBalanceVolume
        let container = ChartDataPointContainer<decimal>($"OBV", DataPointChartType.Line)
        obvContainer |> Array.iter container.Add
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
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! profileResponse = brokerage.GetStockProfile user.State query.Ticker
            let! quoteResponse = brokerage.GetQuote user.State query.Ticker
                
            let view = {
                Ticker = query.Ticker.Value
                Quote = quoteResponse |> Result.toOption
                Profile = profileResponse |> Result.toOption
            }
            
            return Ok view
    }
    
    member _.Handle (query:PriceQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! priceResponse = brokerage.GetQuote user.State query.Ticker
            return priceResponse |> Result.map (_.Price)
    }
    
    member _.Handle (query:PricesQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! priceResponse = brokerage.GetPriceHistory user.State query.Ticker query.Frequency query.Start query.End
            return priceResponse |> Result.map PricesView
    }
    
    member _.Handle (query:QuoteQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user -> return! brokerage.GetQuote user.State query.Ticker
    }
    
    member _.Handle (query:SearchQuery) = task {
        let! user = accounts.GetUser(query.UserId)
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user -> return! brokerage.Search user.State SearchQueryType.Symbol query.Term 10
    }
    
    member _.Handle (query:CompanyFilingsQuery) = task {
        let! response = secFilings.GetFilings(query.Ticker)
        return response |> Result.map (_.Filings)
    }
    
    
