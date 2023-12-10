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


type StockTransaction =
    {
        [<Required>]
        PositionId: StockPositionId
        [<Range(1, 1000000)>]
        NumberOfShares: decimal
        [<Range(0, 100000)>]
        Price: decimal
        [<Required>]
        Date: Nullable<DateTimeOffset>
        StopPrice: Nullable<decimal>
        Notes: string option
        BrokerageOrderId: string option
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
    
type OpenLongStockPosition = {
    [<Range(1, 1000000)>]
    NumberOfShares: decimal
    [<Range(0, 100000)>]
    Price: decimal
    [<Required>]
    Date: DateTimeOffset option
    StopPrice: decimal option
    Notes: string option
    Strategy: string option
    Ticker: Ticker
}
    
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,secFilings:ISECFilings,portfolio:IPortfolioStorage,csvParser:ICSVParser,csvWriter:ICSVWriter) =
    
    interface IApplicationService
    
    member _.Handle(cmd:OpenLongStockPosition,userId:UserId) = task {
        let! user = userId |> accounts.GetUser
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<StockPositionWithCalculations>
        | Some _ ->
            let! stocks = portfolio.GetStockPositions userId
            
            // check if we already have an open position for the ticker
            let openPosition = stocks |> Seq.tryFind (fun x -> x.Ticker = cmd.Ticker && x.Closed = None)
            
            match openPosition with
            | Some _ ->
                return "Position already open" |> ResponseUtils.failedTyped<StockPositionWithCalculations>
            | None ->
                let newPosition =
                    StockPosition.openLong cmd.Ticker cmd.Date.Value
                    |> StockPosition.buy cmd.NumberOfShares cmd.Price cmd.Date.Value cmd.Notes
                    |> StockPosition.setStop cmd.StopPrice cmd.Date.Value
                    |> fun x ->
                        match cmd.Strategy with
                        | Some strategy -> x |> StockPosition.setLabel "strategy" strategy cmd.Date.Value
                        | None -> x
                
                do! newPosition |> portfolio.SaveStockPosition userId openPosition
                
                // check if we have any pending positions for the ticker
                let! pendingPositions = portfolio.GetPendingStockPositions userId
                let pendingPositionOption = pendingPositions |> Seq.tryFind (fun x -> x.State.Ticker = cmd.Ticker && x.State.IsClosed = false)
                
                match pendingPositionOption with
                | Some pendingPosition ->
                    
                    // transfer some data from pending position to this new position
                    let positionWithStop = newPosition |> StockPosition.setStop (Some pendingPosition.State.StopPrice.Value) cmd.Date.Value
                    
                    let positionWithNotes = 
                        match positionWithStop.Notes with
                        | [] when String.IsNullOrWhiteSpace(pendingPosition.State.Notes) = false -> positionWithStop |> StockPosition.addNotes (Some pendingPosition.State.Notes) cmd.Date.Value
                        | _ -> positionWithStop
                    
                    let positionWithStrategy =
                        match pendingPosition.State.Strategy with
                        | null -> positionWithNotes
                        | _ -> positionWithNotes |> StockPosition.setLabel "strategy" pendingPosition.State.Strategy cmd.Date.Value
                        
                    do! positionWithStrategy |> portfolio.SaveStockPosition userId (Some newPosition)
                    
                    let withCalculations =
                        positionWithStrategy
                        |> StockPositionWithCalculations
                    
                    withCalculations.AverageCostPerShare |> pendingPosition.Purchase
                    
                    do! portfolio.SavePendingPosition pendingPosition userId
                    
                    return withCalculations |> ResponseUtils.success
                | None ->
                    return newPosition |> StockPositionWithCalculations |> ResponseUtils.success
    }
    
    member _.Handle(cmd:BuyOrSell) = task {
        
        let buyOrSellFunction =
            match cmd with
            | Buy (data, _) -> StockPosition.buy data.NumberOfShares data.Price data.Date.Value data.Notes
            | Sell (data, _) -> StockPosition.sell data.NumberOfShares data.Price data.Date.Value data.Notes
            
        let data, userId =
            match cmd with
            | Buy (data, userId) -> (data, userId)
            | Sell (data, userId) -> (data, userId)
            
        let! user = accounts.GetUser(userId)
        
        match user with
        | None ->
            return "User not found" |> ResponseUtils.failed
        | _ ->
            let! stock = portfolio.GetStockPosition data.PositionId userId
            
            match stock with
            | None -> return "Stock position not found" |> ResponseUtils.failed
            | Some _ ->
                
                let newState = stock.Value |> buyOrSellFunction
                
                do! portfolio.SaveStockPosition userId stock newState
                
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
                            PositionId = StockPositionId(Guid.NewGuid())
                            NumberOfShares = r.amount
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = None
                            BrokerageOrderId = None
                            Ticker = Ticker(r.ticker)
                        }, cmd.UserId)
                        | "sell" -> Sell({
                            NumberOfShares = r.amount
                            PositionId = StockPositionId(Guid.NewGuid())
                            Price = r.price
                            Date = r.date
                            StopPrice = Nullable<decimal>()
                            Notes = None
                            BrokerageOrderId = None
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
    
    