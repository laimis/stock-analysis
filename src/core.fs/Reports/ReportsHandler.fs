namespace core.fs.Reports

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Globalization
open System.Threading.Tasks
open core.Cryptos
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Options
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis
open core.fs.Services.Trends
open core.fs.Stocks
open core.fs.Stocks.StockPosition
open core.fs.Services

type ChainQuery =
    {
        UserId: UserId
    }
    
type ChainLinkView = 
    {
        success: bool
        ticker: Ticker
        level: int
        profit: decimal
        date: DateTimeOffset
    }
    
type PendingPositionsReportQuery =
    {
        UserId: UserId
    }
    
type ChainView =
    {
        links: ChainLinkView seq
    }
    
type DailyPositionReportQuery =
    {
        UserId: UserId
        PositionId: StockPositionId
    }
    
type DailyTickerReportQuery =
    {
        UserId: UserId
        Ticker: Ticker
        StartDate: string option
        EndDate: string option
    }
    
type GapReportQuery =
    {
        UserId: UserId
        Ticker: Ticker
        Frequency: PriceFrequency
    }
    
type GapReportView =
    {
        gaps: Gap seq
        ticker: string
    }
    
type TransactionsQuery =
    {
        UserId: UserId
        Show:string
        GroupBy:string
        TxType:string
        Ticker:Ticker option
    }
    
type OutcomesReportDuration =
    | SingleBar
    | AllBars
    
    with
        
        override this.ToString() =
            match this with
            | SingleBar -> nameof SingleBar
            | AllBars -> nameof AllBars
            
        static member FromString(value:string) =
            match value with
            | nameof SingleBar -> SingleBar
            | nameof AllBars -> AllBars
            | _ -> failwith $"Unexpected value {value}"
    
type OutcomesReportQuery =
    {
        [<Required>]
        Tickers: Ticker array
        [<Required>]
        Duration: OutcomesReportDuration
        [<Required>]
        Frequency: PriceFrequency
        IncludeGapAnalysis: bool
        StartDate: string
        EndDate: string
    }
    
    with 
        
        member this.DateRange (marketHours:IMarketHours) =
            let tryParse (date:string) func =
                match DateTimeOffset.TryParse(date) with
                | false, _ -> None
                | true, dt -> func(dt) |> Some
            
            let start = tryParse this.StartDate marketHours.GetMarketStartOfDayTimeInUtc
            let ``end`` = tryParse this.EndDate marketHours.GetMarketEndOfDayTimeInUtc
            
            start, ``end``
            
type CorrelationsQuery =
    {
        Days: int
        Tickers: Ticker array
    }

type OutcomesReportForPositionsQuery =
    {
        UserId: UserId
    }

type OutcomesReportViewTickerCountPair =
    {
        Ticker: Ticker
        Count: int
    }
    
type OutcomesReportViewEvaluationCountPair =
    {
        Evaluation: string
        Type: OutcomeType
        Count: int
    }
    
type OutcomesReportView(evaluations,outcomes,gaps,patterns,failed) =
    
    member _.Evaluations: AnalysisOutcomeEvaluation seq = evaluations
    member _.Outcomes: TickerOutcomes seq = outcomes
    member _.Gaps: GapReportView seq = gaps
    member _.Patterns: TickerPatterns seq = patterns
    member _.Failed: string seq = failed
    
    member _.TickerSummary: OutcomesReportViewTickerCountPair seq =
        evaluations
        |> AnalysisOutcomeEvaluationScoringHelper.generateTickerCounts
        |> Seq.map (fun kv -> {Ticker=kv.Key; Count=kv.Value})
        |> Seq.sortByDescending (fun pair -> pair.Count)
    
    member _.EvaluationSummary: OutcomesReportViewEvaluationCountPair seq =
        evaluations
        |> AnalysisOutcomeEvaluationScoringHelper.GenerateEvaluationCounts
        |> Seq.map (fun kv -> (kv, evaluations |> Seq.find (fun e -> e.Name = kv.Key)))
        |> Seq.sortBy (fun (kp, evaluation) -> (evaluation.Type, kp.Value* -1))
        |> Seq.map (fun (kp, evaluation) -> {
            Evaluation=kp.Key
            Type=evaluation.Type
            Count=kp.Value
        })
        
type PercentChangeStatisticsQuery =
    {
        Frequency: PriceFrequency
        Ticker: Ticker
        UserId: UserId
    }
    
type PercentChangeStatisticsView =
    {
        Ticker: string
        Recent: DistributionStatistics
        AllTime: DistributionStatistics
    }
    
type PortfolioCorrelationQuery =
    {
        Days: int
        UserId: UserId
    }
    
type SellsQuery =
    {
        UserId: UserId
    }
    
type SellView =
    {
        Ticker: string
        Date: DateTimeOffset
        NumberOfShares: decimal
        Price: decimal
        OlderThan30Days: bool
        CurrentPrice: decimal option
    }
    
    member this.Age = DateTimeOffset.UtcNow - this.Date
    member this.Diff =
        match this.CurrentPrice with
        | None -> 0m
        | Some value -> (value - this.Price) / this.Price

type TrendsQuery =
    {
        UserId: UserId
        Ticker: Ticker
        TrendType: TrendType
        Start: string option
        End: string option
    }
    
    member this.StartDate (marketHours:IMarketHours) =
        match this.Start with
        | Some date -> match DateTimeOffset.TryParse(date) with | true,dt -> marketHours.GetMarketStartOfDayTimeInUtc dt | false,_ -> DateTimeOffset.UtcNow.AddYears(-10)
        | None -> DateTimeOffset.UtcNow.AddYears(-10)
        
    member this.EndDate (marketHours:IMarketHours) =
        match this.End with
        | Some date -> match DateTimeOffset.TryParse(date) with | true,dt -> marketHours.GetMarketEndOfDayTimeInUtc dt | false,_ -> DateTimeOffset.UtcNow
        | None -> DateTimeOffset.UtcNow

type InflectionPointsQuery =
    {
        UserId: UserId
        Ticker: Ticker
        Start: string option
        End: string option
    }

    member this.StartDate (marketHours:IMarketHours) =
        match this.Start with
        | Some date -> match DateTimeOffset.TryParse(date) with | true,dt -> marketHours.GetMarketStartOfDayTimeInUtc dt | false,_ -> DateTimeOffset.UtcNow.AddYears(-10)
        | None -> DateTimeOffset.UtcNow.AddYears(-10)
        
    member this.EndDate (marketHours:IMarketHours) =
        match this.End with
        | Some date -> match DateTimeOffset.TryParse(date) with | true,dt -> marketHours.GetMarketEndOfDayTimeInUtc dt | false,_ -> DateTimeOffset.UtcNow
        | None -> DateTimeOffset.UtcNow
    
type SellsView(stocks:StockPositionState seq,prices:Dictionary<Ticker,StockQuote>) =
    
    member _.Sells: SellView seq =
        stocks
        |> Seq.collect (fun stock -> stock.Transactions |> Seq.map (fun t -> {|stock = stock; buyOrSell = t|}))
        |> Seq.map (fun t -> match t.buyOrSell with | Share s -> Some s | _ -> None)
        |> Seq.choose id
        |> Seq.filter (fun t -> t.Type = Sell && t.Date > DateTimeOffset.UtcNow.AddDays(-60))
        |> Seq.groupBy _.Ticker
        |> Seq.map (fun (ticker, sells) -> {|ticker = ticker; latest = sells |> Seq.maxBy (fun t -> t.Date)|})
        |> Seq.map (fun t -> {
            Ticker = t.ticker.Value
            Date = t.latest.Date
            NumberOfShares = t.latest.NumberOfShares
            Price = t.latest.Price
            OlderThan30Days = t.latest.Date < DateTimeOffset.UtcNow.AddDays(-30)
            CurrentPrice =
                match prices.TryGetValue(t.ticker) with
                | true, q -> Some q.Price
                | false, _ -> None
        })
        
type WeeklySummaryQuery =
    {
        Period: string
        UserId: UserId
    }
    
    member this.GetDates() =
        let start = DateTimeOffset.UtcNow.Date.AddDays(-7)
        let ``end`` = DateTimeOffset.UtcNow.Date.AddDays(1);

        match this.Period with
        | "last7days" ->
            (start, ``end``)
        | _ ->
            let date = DateTimeOffset.UtcNow.Date;
            let offset = int date.DayOfWeek - 1;
            let toSubtract =
                match offset with
                | x when x < 0 -> 6
                | _ -> offset

            let monday = date.AddDays(-1.0 * (toSubtract |> float));
            let sunday = monday.AddDays(7)
            
            (monday, sunday)
            
type WeeklySummaryView(
    start,
    ``end``,
    openedStocks:StockPositionWithCalculations list,
    closedStocks:StockPositionWithCalculations list,
    stockTransactions:PLTransaction list,
    optionTransactions:Transaction list,
    plStockTransactions:PLTransaction list,
    openedOptions:OptionPositionState list,
    closedOptions:OptionPositionState list,
    dividends:StockPositionDividendTransaction list,
    fees:StockPositionFeeTransaction list) =
        
        let optionMultipler= 100m
        
        member _.Start = start
        member _.End = ``end``
        member _.OpenedStocks = openedStocks
        member _.ClosedStocks = closedStocks
        member _.StockTransactions = stockTransactions
        member _.OptionTransactions = optionTransactions
        member _.PLStockTransactions = plStockTransactions
        member _.OpenedOptions = openedOptions |> List.map (fun o -> OptionPositionView(o, None))
        member _.ClosedOptions = closedOptions |> List.map (fun o -> OptionPositionView(o, None))
        member _.Dividends = dividends
        member _.Fees = fees
        
        member _.StockProfit =
            plStockTransactions
            |> Seq.sumBy (fun (t:PLTransaction) -> t.Profit)
        member _.DividendProfit =
            dividends
            |> Seq.sumBy (fun (t:StockPositionDividendTransaction) -> t.NetAmount)
        member _.FeeProfit =
            fees
            |> Seq.sumBy (fun (t:StockPositionFeeTransaction) -> t.NetAmount)
        member this.OptionProfit = this.ClosedOptions |> Seq.sumBy _.Profit |> fun x -> x * optionMultipler
        member this.TotalProfit = this.StockProfit + this.DividendProfit + this.FeeProfit + this.OptionProfit


    
type TransactionGroup(name:string,transactions:Transaction seq) =
    let sum = transactions |> Seq.sumBy _.Amount
    member _.Name = name
    member _.Transactions = transactions
    member _.Sum = sum

type TransactionsView(transactions:Transaction seq, groupBy:string, tickers:Ticker array) =
    
    let groupByValue (groupBy:string) (t:Transaction) =
        match groupBy with
        | "ticker" -> t.Ticker.Value
        | "week" -> t.DateAsDate.AddDays(- float t.DateAsDate.DayOfWeek+1.0).ToString("MMMM dd, yyyy")
        | "year" -> t.DateAsDate.ToString("yyyy")
        | _ -> t.DateAsDate.ToString("MMMM, yyyy")
        
    let ordered groupBy (transactions:Transaction seq) =
        match groupBy with
        | "ticker" -> transactions |> Seq.sortBy _.Ticker
        | _ -> transactions |> Seq.sortByDescending _.DateAsDate
    
    // build the P/L chart data where we go through each transaction and maintain a running total and
    // the date at which this total was calculated
    let plChartData title filter (txs:Transaction seq) =
        let zeroLineAnnotation = ChartAnnotationLine(0, ChartAnnotationLineType.Horizontal) |> Option.Some
        let plDataContainer = ChartDataPointContainer<decimal>(title, DataPointChartType.Line, zeroLineAnnotation)
        let _ =
            txs
            |> Seq.filter filter
            |> Seq.groupBy _.DateAsDate.Date
            |> Seq.map (fun (date, ts) -> date, ts |> Seq.sumBy _.Amount)
            |> Seq.sortBy fst
            |> Seq.fold (fun total (txDate,amount) ->
                let newTotal = total + amount
                plDataContainer.Add(txDate, newTotal)
                newTotal
            ) 0m
        
        plDataContainer
        
    let drawdownChartData title filter (txs:Transaction seq) =
        // go over txs after applying a filter on them and generate a running drawdown
        // chart data. The drawdown is calculated as the difference between the current
        // total and the maximum total so far
        let drawdownDataContainer = ChartDataPointContainer<decimal>(title, DataPointChartType.Line, None)
        let _ =
            txs
            |> Seq.filter filter
            |> Seq.groupBy _.DateAsDate.Date
            |> Seq.map (fun (date, ts) -> date, ts |> Seq.sumBy _.Amount)
            |> Seq.sortBy fst
            |> Seq.fold (fun total (txDate,amount) ->
                // if new total is positive, keep total at 0, we are only interested in drawdowns, so to speak
                let newTotal = 
                    match total + amount with
                    | x when x > 0m -> 0m
                    | x -> x
                
                drawdownDataContainer.Add(txDate, total)
                    
                newTotal
                
            ) 0m
        
        drawdownDataContainer
    
    let availableYears = transactions |> Seq.map (fun t -> t.DateAsDate.Year) |> Seq.distinct |> Seq.sortDescending |> Seq.toArray
    
    let plBreakdowns =
        availableYears
        |> Array.collect (fun year ->
            [|
                plChartData $"P/L {year}" (fun (t:Transaction)-> t.DateAsDate.Year = year) transactions
                drawdownChartData $"Drawdown {year}" (fun (t:Transaction)-> t.DateAsDate.Year = year) transactions
            |]
        )
        
    let grouped =
        match groupBy with
        | null  -> Seq.empty<TransactionGroup>
        | _ -> 
            let grouped =
                transactions
                |> ordered groupBy
                |> Seq.groupBy (groupByValue groupBy)
                |> Seq.map TransactionGroup
            
            match groupBy with
            | "ticker" -> grouped |> Seq.sortByDescending (fun g -> g.Transactions |> Seq.sumBy _.Amount)
            | _ -> grouped
            
    let tickerSymbols = tickers |> Array.map _.Value
    let credit = transactions |> Seq.sumBy _.Amount
    let debit = transactions |> Seq.sumBy _.Amount
    
    member _.Transactions = ordered groupBy transactions
    member _.Tickers = tickerSymbols
    member _.Grouped = grouped
    member _.Credit = credit
    member _.Debit = debit
    member _.PLBreakdowns = plBreakdowns

type ReportsHandler(accounts:IAccountStorage,brokerage:IBrokerage,marketHours:IMarketHours,storage:IPortfolioStorage) =
    
    let getLevel (position:StockPositionWithCalculations) =
        match position.Profit |> abs with
        | profit when profit > 1000m -> 5
        | profit when profit > 500m -> 4
        | profit when profit > 100m -> 3
        | profit when profit > 50m -> 2
        | profit when profit > 10m -> 1
        | _ -> 0
        
    let dailyBreakdownReport userState ticker start end' position = task {
            
        let! pricesResponse =
            brokerage.GetPriceHistory
                userState
                ticker
                PriceFrequency.Daily
                start
                end'
                
        return pricesResponse |> Result.map (fun prices ->
            PositionAnalysis.dailyPLAndGain prices position
        )
    }
    
    let runCorrelations userId numberOfDays tickers = task {
        let! user = accounts.GetUser userId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! prices =
                tickers
                |> Seq.map (fun ticker -> async {
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            ticker
                            PriceFrequency.Daily
                            (DateTimeOffset.UtcNow.AddDays(-numberOfDays) |> Some)
                            None
                        |> Async.AwaitTask
                    match priceResponse with
                    | Error _ -> return None
                    | Ok prices -> return Some (ticker, prices)
                })
                |> Async.Sequential
                
            let successOnly = prices |> Array.choose id
            
            let matrix = successOnly |> Array.map snd |> Array.map _.Bars
            
            let correlations = PositionAnalysis.correlations matrix
            
            return successOnly
            |> Array.mapi (fun index (ticker, _) ->
                let row = correlations.GetValue(index) :?> float[]
                let averageCorrelationMinusItself =
                    row |> Array.sum |> fun sum -> (sum - row[index]) / (float (row.Length - 1))
                {|ticker=ticker; correlations=row; averageCorrelation=averageCorrelationMinusItself|}
            ) |> Ok
    }
    
    interface IApplicationService
    
    member _.Handle(request:PortfolioCorrelationQuery) = task {
        let! stocks = storage.GetStockPositions request.UserId
        let! options = storage.GetOptionPositions request.UserId
            
        let tickers = 
            stocks
            |> Seq.filter _.IsOpen
            |> Seq.map _.Ticker
            |> Seq.append (options |> Seq.filter _.IsOpen |> Seq.map _.UnderlyingTicker)
            |> Seq.distinct
            
        return! runCorrelations request.UserId request.Days tickers
    }
    
    member _.HandleCorrelationsQuery userId (query:CorrelationsQuery) = task {
        return! runCorrelations userId query.Days query.Tickers
    }
    
    member _.Handle(request:ChainQuery) = task {
        let! stocks = storage.GetStockPositions request.UserId
        
        let links =
            stocks
            |> Seq.filter _.IsClosed
            |> Seq.sortByDescending _.Closed.Value
            |> Seq.map StockPositionWithCalculations
            |> Seq.map (fun position ->
                {
                    success=position.Profit > 0m
                    ticker=position.Ticker
                    level = position |> getLevel
                    profit = position.Profit
                    date = position.Closed.Value
                }
            )
            
        return {links=links}
    }
    
    member _.Handle(request:DailyPositionReportQuery) = task {
        
        let! user = accounts.GetUser request.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! stock = storage.GetStockPosition request.PositionId request.UserId
            
            match stock with
            | None -> return "Stock position not found" |> ServiceError |> Error
            | Some position ->
                
                let position = position |> StockPositionWithCalculations
                
                let start = position.Opened |> marketHours.GetMarketStartOfDayTimeInUtc |> Some
                let ``end`` =
                    match position.Closed with
                    | Some closed -> closed |> marketHours.GetMarketEndOfDayTimeInUtc |> Some
                    | None -> None
                
                return! dailyBreakdownReport user.State position.Ticker start ``end`` (position |> Some)                   
    }
    
    member _.Handle (query:DailyTickerReportQuery) = task {
        
        let start =
            match query.StartDate with
            | None -> DateTimeOffset.UtcNow.AddDays(-30) |> Some
            | Some date when String.IsNullOrEmpty(date) -> DateTimeOffset.UtcNow.AddDays(-30) |> Some
            | Some date -> DateTimeOffset.Parse(date, CultureInfo.InvariantCulture) |> Some
            
        let end' =
            match query.EndDate with
            | None -> DateTimeOffset.UtcNow |> Some
            | Some date when String.IsNullOrEmpty(date) -> DateTimeOffset.UtcNow |> Some
            | Some date -> DateTimeOffset.Parse(date, CultureInfo.InvariantCulture) |> Some
        
        let! user = accounts.GetUser query.UserId
        let ticker = query.Ticker
                
        return! dailyBreakdownReport user.Value.State ticker start end' None
    }
    
    member _.Handle (query:GapReportQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! priceResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    query.Frequency
                    None
                    None
            
            return priceResponse |> Result.map (fun prices ->
                let gaps = prices |> detectGaps Constants.NumberOfDaysForRecentAnalysis
                {gaps=gaps; ticker=query.Ticker.Value}
            )
    }
    
    member _.HandleOutcomesReport userId (query:OutcomesReportQuery) = task {
        let! user = accounts.GetUser userId
        
        let startDate, endDate = query.DateRange marketHours
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! tasks =
                query.Tickers
                |> Seq.map( fun t -> async {
                    
                    try
                        
                        let! priceResponse =
                            brokerage.GetPriceHistory
                                user.State
                                t
                                query.Frequency
                                startDate
                                endDate
                            |> Async.AwaitTask
                        
                        match priceResponse with
                        | Error e -> return Error (t,e.Message) 
                        | Ok prices ->
                            
                            let outcomes =
                                match query.Duration with
                                | OutcomesReportDuration.SingleBar ->  SingleBarPriceAnalysis.run prices
                                | OutcomesReportDuration.AllBars -> MultipleBarPriceAnalysis.run prices
                            
                            let tickerOutcome:TickerOutcomes = {outcomes=outcomes; ticker=t}
                            
                            let gapsView =
                                match query.IncludeGapAnalysis with
                                | true ->
                                    let gaps = prices |> detectGaps Constants.NumberOfDaysForRecentAnalysis
                                    Some {gaps=gaps; ticker=t.Value}
                                | false -> None
                                
                            let patterns = PatternDetection.generate prices
                            let tickerPatterns = {patterns = patterns; ticker = t}
                            
                            return (tickerOutcome, gapsView, tickerPatterns) |> Ok
                    with
                    | exn ->
                        return Error (t, exn.Message)
                    }
                )
                |> Async.Sequential
                |> Async.StartAsTask
                
            let outcomes, gaps, patterns =
                tasks
                |> Seq.map Result.toOption
                |> Seq.choose id
                |> Seq.fold (fun (outcomes, gaps, patterns) (outcome, gap, pattern) ->
                    let newOutcomes = outcomes @ [outcome]
                    let newGaps = gaps @ [gap]
                    let newPatterns = patterns @ [pattern]
                    (newOutcomes, newGaps, newPatterns)
                ) ([], [], [])
                
            let cleanedGaps = gaps |> Seq.choose id
            
            let failed = tasks |> Seq.map (fun t -> match t with | Error (t,e) -> Some $"{t}: {e}" | _ -> None) |> Seq.choose id
            
            let evaluations =
                match query.Duration with
                | OutcomesReportDuration.SingleBar -> SingleBarPriceAnalysisEvaluation.evaluate outcomes
                | OutcomesReportDuration.AllBars ->  MultipleBarPriceAnalysis.evaluate outcomes
            
            return OutcomesReportView(evaluations, outcomes, cleanedGaps, patterns, failed) |> Ok
    }
    
    member _.Handle (query:PendingPositionsReportQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! stocks = storage.GetPendingStockPositions query.UserId
            
            let active = stocks |> Seq.filter (fun p -> p.State.IsOpen)
            
            let! tasks =
                active
                |> Seq.map(fun p -> async {
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            p.State.Ticker
                            PriceFrequency.Daily
                            None
                            None
                        |> Async.AwaitTask
                    
                    match priceResponse with
                    | Error error -> return Error (p, error.Message) 
                    | Ok prices ->
                        
                        try    
                            let outcomes = PendingPositionAnalysis.generate p.State prices
                            return Ok (p, outcomes)
                        with
                        | exn ->
                            return Error (p, exn.Message)
                })
                |> Async.Sequential
            
            let outcomes =
                tasks
                |> Seq.map Result.toOption
                |> Seq.choose id
                |> Seq.fold (fun outcomes (_, outcome) ->
                    let newOutcomes = outcomes @ [outcome]
                    newOutcomes
                ) []
                
            let failed = tasks |> Seq.map (fun t -> match t with | Error (position,message) -> Some $"{position.State.Ticker}: {message}" | _ -> None) |> Seq.choose id
                
            let evaluations = PendingPositionAnalysis.evaluate outcomes
            
            return OutcomesReportView(evaluations, outcomes, [], [], failed) |> Ok
    }
    
    member _.Handle (query:OutcomesReportForPositionsQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! stocks = query.UserId |> storage.GetStockPositions
            let positions = stocks |> Seq.filter (fun stock -> stock.IsClosed |> not)
            
            let! account = brokerage.GetAccount user.State
            let brokerageAccount =
                match account with
                | Error _ -> BrokerageAccount.Empty
                | Ok account -> account
                
            // we also want to generate temporary positions from positions that are
            // in brokerage and have a matching pending position that's open,
            // it means the position is baking and I am evaluating if to keep it
            let! pendingStockPositions = storage.GetPendingStockPositions query.UserId
            let pendingStockPositions = pendingStockPositions |> Seq.filter (fun p -> p.State.IsClosed |> not)
            
            let brokerageStockPositions =
                brokerageAccount.StockPositions
                |> Array.filter (fun p -> positions |> Seq.tryFind (fun s -> s.Ticker = p.Ticker) |> Option.isNone)
                |> Array.map (fun p ->
                    match pendingStockPositions |> Seq.tryFind (fun s -> s.State.Ticker = p.Ticker) with
                    | Some pending -> Some (p, pending.State)
                    | None -> None
                )
                |> Array.choose id
                |> Array.map (fun (brokeragePosition, pendingPosition) ->
                    
                    // let's look if we can find the orders for this position in the brokerage account
                    let firstOrder =
                        brokerageAccount.StockOrders
                        |> Array.filter (fun o -> o.Ticker = brokeragePosition.Ticker && o.ExecutionTime.IsSome)
                        |> Array.sortBy (fun o -> o.ExecutionTime.Value)
                        |> Array.tryHead
                        
                    let openDate =
                        match firstOrder with
                        | Some order -> order.ExecutionTime.Value
                        | None -> DateTimeOffset.UtcNow
                    
                    let position =
                        StockPosition.``open`` pendingPosition.Ticker pendingPosition.NumberOfShares brokeragePosition.AverageCost openDate
                        |> setStop pendingPosition.StopPrice openDate
                        |> setLabel "strategy" pendingPosition.Strategy openDate
                        
                    position
                )
                
            let! tasks =
                positions
                |> Seq.append brokerageStockPositions
                |> Seq.filter (fun p -> p.HasLabel "strategy" "longterminterest" |> not)
                |> Seq.map (fun position -> async {
                    
                    let calculations = position |> StockPositionWithCalculations
                    
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            position.Ticker
                            PriceFrequency.Daily
                            None
                            None
                        |> Async.AwaitTask
                    
                    match priceResponse with
                    | Error error -> return Error (position, error.Message) 
                    | Ok prices ->
                        
                        try    
                            let outcomes = PositionAnalysis.generate calculations prices brokerageAccount.StockOrders
                            let tickerOutcome:TickerOutcomes = {outcomes = outcomes; ticker = position.Ticker}
                            let tickerPatterns = {patterns = PatternDetection.generate prices; ticker = position.Ticker}
                            
                            return Ok (tickerOutcome, tickerPatterns)
                        with
                        | exn ->
                            return Error (position, exn.Message)
                    }
                )
                |> Async.Sequential
                |> Async.StartAsTask
                
            let outcomes, patterns =
                tasks
                |> Seq.map Result.toOption
                |> Seq.choose id
                |> Seq.fold (fun (outcomes, patterns) (outcome, pattern) ->
                    let newOutcomes = outcomes @ [outcome]
                    let newPatterns = patterns @ [pattern]
                    (newOutcomes, newPatterns)
                ) ([], [])
                
            let failed = tasks |> Seq.map (fun t -> match t with | Error (position,message) -> Some $"{position.Ticker}: {message}" | _ -> None) |> Seq.choose id
                
            let evaluations = PositionAnalysis.evaluate user.State outcomes
            
            return OutcomesReportView(evaluations, outcomes, [], patterns, failed) |> Ok
    }
    
    member _.Handle (query:PercentChangeStatisticsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! pricesResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    query.Frequency
                    None
                    None
            
            return pricesResponse |> Result.map (fun prices ->
                let recent = 30 |> prices.LatestOrAll |> PercentChangeAnalysis.calculateForPriceBars true
                let allTime = prices |> PercentChangeAnalysis.calculateForPriceBars true
                {Ticker=query.Ticker.Value; Recent=recent; AllTime=allTime}
            )
    }
    
    member _.Handle (query:SellsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! stocks = storage.GetStockPositions query.UserId
            
            let! priceResult =
                brokerage.GetQuotes
                    user.State
                    (stocks |> Seq.map _.Ticker)
            
            let prices = priceResult |> Result.defaultValue (Dictionary<Ticker,StockQuote>())
                
            return SellsView(stocks, prices) |> Ok
    }

    member _.Handle (query:InflectionPointsQuery) = task {
        let start = query.StartDate marketHours
        let ``end`` = query.EndDate marketHours
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! priceResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    Daily
                    (start |> Some)
                    (``end`` |> Some)
            
            return priceResponse |> Result.map (fun prices ->
                
                let priceInflectionPoints = InflectionPoints.calculateInflectionPoints prices.Bars
                let priceTrendAnalysis = InflectionPoints.getCompleteTrendAnalysis priceInflectionPoints prices.Last
                
                let obv = MultipleBarPriceAnalysis.Indicators.onBalanceVolume prices
                let normalized = Analysis.DataPointHelpers.normalize obv
                let normalizedAsBars = Analysis.DataPointHelpers.toPriceBars normalized
                let obvInflectionPoints = InflectionPoints.calculateInflectionPoints normalizedAsBars
                let obvTrendAnalysis = InflectionPoints.getCompleteTrendAnalysis obvInflectionPoints normalizedAsBars[normalizedAsBars.Length - 1]

                {|
                    priceInflectionPoints = priceInflectionPoints;
                    priceTrendAnalysis = priceTrendAnalysis;
                    obvInflectionPoints = obvInflectionPoints;
                    obvTrendAnalysis = obvTrendAnalysis;
                    prices = prices.Bars;
                    obv = normalizedAsBars
                |}
            )
    }


    member _.Handle (query:TrendsQuery) = task {
        let start = query.StartDate marketHours
        let ``end`` = query.EndDate marketHours
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! priceResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    PriceFrequency.Daily
                    (start |> Some)
                    (``end`` |> Some)
            
            return priceResponse |> Result.map (fun prices ->
                let trends = TrendCalculator.generate query.Ticker query.TrendType prices
                trends
            )
    }
    
    member _.Handle (query:WeeklySummaryQuery) = task {
        
        let! stocks = storage.GetStockPositions query.UserId
        let! options = storage.GetOptionPositions query.UserId
        let start, ``end`` = query.GetDates()
        
        let stocks = stocks |> Seq.map StockPositionWithCalculations |> Seq.toArray
             
        let optionTransactions =
            options
            |> Seq.map (fun o ->
                o.Transactions |> List.map (fun t -> o, t)
            )
            |> Seq.collect id
            |> Seq.filter (fun (_,t) -> t.When >= DateTimeOffset(start))
            |> Seq.map (fun (s,t) ->
                
                let aggregateId = OptionPositionId.guid s.PositionId
                let amount =
                    match t.Credited with
                    | Some value -> value
                    | None -> t.Debited |> Option.defaultValue 0m
                
                Transaction.NonPLTx(aggregateId, t.EventId, s.UnderlyingTicker, "tbd", amount, amount, t.When, true)
            )
            |> Seq.toList
            
        let plStockTransactions =
            stocks
            |> Seq.collect _.PLTransactions
            |> Seq.filter (fun t -> t.Date >= DateTimeOffset(start))
            |> Seq.sortBy _.Ticker
            |> Seq.toList
            
        let dividends =
            stocks
            |> Seq.collect _.Dividends
            |> Seq.filter (fun d -> d.Date >= DateTimeOffset(start))
            |> Seq.sortBy _.Ticker
            |> Seq.toList
            
        let fees =
            stocks
            |> Seq.collect _.Fees
            |> Seq.filter (fun f -> f.Date >= DateTimeOffset(start))
            |> Seq.sortBy _.Ticker
            |> Seq.toList
            
        let closedOptions =
            options
            |> Seq.filter (fun s -> s.IsClosed && s.Closed.Value >= DateTimeOffset(start) && s.Closed.Value <= DateTimeOffset(``end``))
            |> Seq.sortBy _.UnderlyingTicker
            |> Seq.toList
            
        let openedOptions =
            options
            |> Seq.filter (fun s -> s.IsOpen && s.Opened.Value >= DateTimeOffset(start) && s.Opened.Value <= DateTimeOffset(``end``))
            |> Seq.sortBy _.UnderlyingTicker
            |> Seq.toList
           
        let closedStocks =
            stocks
            |> Seq.filter (fun p -> p.IsClosed && p.Closed.Value >= DateTimeOffset(start) && p.Closed.Value <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let openedStocks =
            stocks
            |> Seq.filter (fun p -> p.IsClosed = false && p.Opened >= DateTimeOffset(start) && p.Opened <= DateTimeOffset(``end``))
            |> Seq.toList
            
        let view = WeeklySummaryView(
            start=start,
            ``end``=``end``,
            openedStocks=openedStocks,
            closedStocks=closedStocks,
            stockTransactions=List.empty<PLTransaction>,
            optionTransactions=optionTransactions,
            plStockTransactions=plStockTransactions,
            openedOptions=openedOptions,
            closedOptions=closedOptions,
            dividends=dividends,
            fees=fees
        )
        
        return view
    }
    
    member _.Handle(query:TransactionsQuery) : Task<Result<TransactionsView, ServiceError>>  = task {
        
        let toSharedTransaction (stock:StockPositionWithCalculations) (plTransaction:PLTransaction) : Transaction =
            Transaction.PLTx(Guid.NewGuid(), stock.Ticker, $"{plTransaction.Type} {plTransaction.NumberOfShares} shares", plTransaction.BuyPrice, plTransaction.Profit, plTransaction.Date, false)
            
        let toSharedTransactionForDividend (stock:StockPositionWithCalculations) (dividend:StockPositionDividendTransaction) : Transaction =
            Transaction.PLTx(Guid.NewGuid(), stock.Ticker, "Dividend", dividend.NetAmount, dividend.NetAmount, dividend.Date, false)
            
        let toSharedTransactionForFee (stock:StockPositionWithCalculations) (fee:StockPositionFeeTransaction) : Transaction =
            Transaction.PLTx(Guid.NewGuid(), stock.Ticker, "Fee", fee.NetAmount, fee.NetAmount, fee.Date, false)
            
        let toSharedTransactionForOption (o:OptionPositionState) =
            let minStrike = o.Contracts |> Seq.minBy _.Key.Strike
            let maxStrike = o.Contracts |> Seq.maxBy _.Key.Strike
            let spread = (maxStrike.Key.Strike - minStrike.Key.Strike) * 100m |> int
            let debitOrCredit = o.Cost |> Option.defaultValue 0m |> fun x -> if x > 0m then "Debit" else "Credit"
            let callOrPut = o.Contracts |> Seq.head |> fun x -> if x.Key.OptionType = Call then "Call" else "Put"
            let spreadDescription =
                match spread with
                | x when x = 0 -> $"{callOrPut} option contract"
                | _ -> $"{spread} {debitOrCredit} {callOrPut} spread"
            let description = $"{o.UnderlyingTicker} {spreadDescription}"
            Transaction.PLTx(o.PositionId |> OptionPositionId.guid, o.UnderlyingTicker, description, o.Cost |> Option.defaultValue 0m, o.Profit * 100m, o.Closed.Value, true)
            
        let toTransactionsView (stockPositions:StockPositionState seq) (options:OptionPositionState seq) (cryptos:OwnedCrypto seq) =
            let stocks = stockPositions |> Seq.map StockPositionWithCalculations |> Seq.toArray
            let tickers =
                stocks
                |> Seq.map _.Ticker
                |> Seq.append (options |> Seq.map _.UnderlyingTicker)
                |> Seq.distinct
                |> Seq.sort
                |> Seq.toArray
            
            let stockTransactions =
                match query.Show = "stocks" || query.Show = "stocksandoptions" || query.Show = null with
                | true ->
                    stocks
                    |> Seq.filter (fun s -> query.Ticker.IsNone || s.Ticker = query.Ticker.Value)
                    |> Seq.collect (fun s -> s.PLTransactions |> Seq.map (toSharedTransaction s))
                | false -> Seq.empty
                
            let stockDividends =
                match query.Show = "dividends" || query.Show = null with
                | true ->
                    stocks
                    |> Seq.filter (fun s -> query.Ticker.IsNone || s.Ticker = query.Ticker.Value)
                    |> Seq.collect (fun s -> s.Dividends |> Seq.map (toSharedTransactionForDividend s))
                | false -> Seq.empty
                
            let stockFees =
                match query.Show = "fees" || query.Show = null with
                | true ->
                    stocks
                    |> Seq.filter (fun s -> query.Ticker.IsNone || s.Ticker = query.Ticker.Value)
                    |> Seq.collect (fun s -> s.Fees |> Seq.map (toSharedTransactionForFee s))
                | false -> Seq.empty
            
            let optionTransactions =
                match query.Show = "options" || query.Show = "stocksandoptions" || query.Show = null with
                | true ->
                    options
                    |> Seq.filter (fun o -> o.IsClosed && (query.Ticker.IsNone || o.UnderlyingTicker = query.Ticker.Value))
                    |> Seq.map toSharedTransactionForOption
                | false -> Seq.empty
                
            let cryptoTransactions =
                match query.Show = "cryptos" || query.Show = null with
                | true ->
                    cryptos
                    |> Seq.filter (fun c -> query.Ticker.IsNone || c.State.Token = query.Ticker.Value.Value)
                    |> Seq.collect _.State.Transactions
                    |> Seq.map _.ToSharedTransaction()
                    |> Seq.filter (fun t -> if query.TxType = "pl" then t.IsPL else t.IsPL |> not)
                | false -> Seq.empty
                
            let log = stockTransactions |> Seq.append stockDividends |> Seq.append stockFees |> Seq.append optionTransactions |> Seq.append cryptoTransactions
                
            TransactionsView(log, query.GroupBy, tickers);
            
        let! stocks = storage.GetStockPositions(query.UserId)
        let! options = storage.GetOptionPositions(query.UserId)
        let! cryptos = storage.GetCryptos(query.UserId)
        
        let transactionsView = toTransactionsView stocks options cryptos 
        
        return transactionsView |> Ok
    }
