namespace core.fs.Reports

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis
open core.fs.Services.MultipleBarPriceAnalysis
open core.fs.Stocks

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
    
type ChainView =
    {
        links: ChainLinkView seq
    }
    
type DailyOutcomeScoreReportView =
    {
        Ticker: Ticker
        DailyScores: ChartDataPointContainer<int>
    }
    
type DailyPositionReportQuery =
    {
        UserId: UserId
        Ticker: Ticker
        PositionId: StockPositionId
    }
    
type DailyPositionReportView =
    {
        DailyProfit: ChartDataPointContainer<decimal>
        DailyGainPct: ChartDataPointContainer<decimal>
        Ticker: string
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
    
type OutcomesReportView(evaluations,outcomes,gaps,patterns) =
    
    member _.Evaluations: AnalysisOutcomeEvaluation seq = evaluations
    member _.Outcomes: TickerOutcomes seq = outcomes
    member _.Gaps: GapReportView seq = gaps
    member _.Patterns: TickerPatterns seq = patterns
    
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
        
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,marketHours:IMarketHours,storage:IPortfolioStorage) =
    
    let getLevel (position:StockPositionWithCalculations) =
        match position.Profit |> abs with
        | profit when profit > 1000m -> 5
        | profit when profit > 500m -> 4
        | profit when profit > 100m -> 3
        | profit when profit > 50m -> 2
        | profit when profit > 10m -> 1
        | _ -> 0
        
    interface IApplicationService
    
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
                
                let start = position.Opened |> marketHours.GetMarketStartOfDayTimeInUtc
                let ``end`` =
                    match position.Closed with
                    | Some closed -> closed |> marketHours.GetMarketEndOfDayTimeInUtc
                    | None -> DateTimeOffset.MinValue
                
                let! pricesResponse =
                    brokerage.GetPriceHistory
                        user.State
                        request.Ticker
                        PriceFrequency.Daily
                        start
                        ``end``
                        
                return pricesResponse |> Result.map (fun prices ->
                    let profit, pct = PositionAnalysis.dailyPLAndGain prices position
                    
                    {
                        DailyProfit = profit
                        DailyGainPct = pct
                        Ticker = request.Ticker.Value
                    }
                )                   
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
                    DateTimeOffset.MinValue
                    DateTimeOffset.MinValue
            
            return priceResponse |> Result.map (fun prices ->
                let gaps = prices |> detectGaps Constants.NumberOfDaysForRecentAnalysis
                {gaps=gaps; ticker=query.Ticker.Value}
            )
    }
    
    member _.HandleOutcomesReport userId (query:OutcomesReportQuery) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            
            let! tasks =
                query.Tickers
                |> Seq.map( fun t -> async {
                    let startDate =
                        match DateTimeOffset.TryParse(query.StartDate) with
                        | false, _ -> DateTimeOffset.MinValue
                        | true, dt -> marketHours.GetMarketStartOfDayTimeInUtc(dt)
                    
                    let endDate =
                        match DateTimeOffset.TryParse(query.EndDate) with
                        | false, _ -> DateTimeOffset.MinValue
                        | true, dt -> marketHours.GetMarketEndOfDayTimeInUtc(dt)
                    
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
                        | Error _ -> return None
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
                            
                            return Some (tickerOutcome, gapsView, tickerPatterns)
                    with
                    | _ ->
                        return None
                    }
                )
                |> Async.Sequential
                |> Async.StartAsTask
                
            let outcomes, gaps, patterns =
                tasks
                |> Seq.choose id
                |> Seq.fold (fun (outcomes, gaps, patterns) (outcome, gap, pattern) ->
                    let newOutcomes = outcomes @ [outcome]
                    let newGaps = gaps @ [gap]
                    let newPatterns = patterns @ [pattern]
                    (newOutcomes, newGaps, newPatterns)
                ) ([], [], [])
                
            let cleanedGaps = gaps |> Seq.choose id
            
            let evaluations =
                match query.Duration with
                | OutcomesReportDuration.SingleBar -> SingleBarPriceAnalysisEvaluation.evaluate outcomes
                | OutcomesReportDuration.AllBars -> MultipleBarAnalysisOutcomeEvaluation.evaluate outcomes
            
            return OutcomesReportView(evaluations, outcomes, cleanedGaps, patterns) |> Ok
    }
    
    member _.Handle (query:OutcomesReportForPositionsQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ServiceError |> Error
        | Some user ->
            let! stocks = query.UserId |> storage.GetStockPositions
            
            let positions = stocks |> Seq.filter (fun stock -> stock.IsClosed |> not)
            
            let! account = brokerage.GetAccount user.State
            let orders =
                match account with
                | Error _ -> Array.Empty<Order>()
                | Ok account -> account.Orders
                
            let! tasks =
                positions
                |> Seq.map (fun position -> async {
                    
                    let calculations = position |> StockPositionWithCalculations
                    
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            position.Ticker
                            PriceFrequency.Daily
                            position.Opened
                            DateTimeOffset.UtcNow
                        |> Async.AwaitTask
                    
                    match priceResponse with
                    | Error _ -> return None
                    | Ok prices ->
                        
                        let outcomes = PositionAnalysis.generate calculations prices orders
                        let tickerOutcome:TickerOutcomes = {outcomes = outcomes; ticker = position.Ticker}
                        let tickerPatterns = {patterns = PatternDetection.generate prices; ticker = position.Ticker}
                        
                        return Some (tickerOutcome, tickerPatterns)
                    }
                )
                |> Async.Sequential
                |> Async.StartAsTask
                
            let outcomes, patterns =
                tasks
                |> Seq.choose id
                |> Seq.fold (fun (outcomes, patterns) (outcome, pattern) ->
                    let newOutcomes = outcomes @ [outcome]
                    let newPatterns = patterns @ [pattern]
                    (newOutcomes, newPatterns)
                ) ([], [])
                
            let evaluations = PositionAnalysis.evaluate outcomes
            
            return OutcomesReportView(evaluations, outcomes, [], patterns) |> Ok
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
                    DateTimeOffset.MinValue
                    DateTimeOffset.MinValue
            
            return pricesResponse |> Result.map (fun prices ->
                let recent = 30 |> prices.LatestOrAll |> PercentChangeAnalysis.calculateForPriceBars
                let allTime = prices |> PercentChangeAnalysis.calculateForPriceBars
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
                    (stocks |> Seq.map (_.Ticker))
            
            let prices =
                match priceResult with
                | Error _ -> Dictionary<Ticker,StockQuote>()
                | Ok prices -> prices
                
            return SellsView(stocks, prices) |> Ok
    }
