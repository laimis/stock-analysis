namespace core.fs.Reports

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Stocks
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Services.Analysis.SingleBarPriceAnalysis
open core.fs.Services.GapAnalysis
open core.fs.Services.MultipleBarPriceAnalysis
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts

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
        PositionId: int
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
        CurrentPrice: Nullable<decimal>
    }
    
    member this.Age = DateTimeOffset.UtcNow - this.Date
    member this.Diff =
        match this.CurrentPrice.HasValue with
        | false -> 0m
        | true -> (this.CurrentPrice.Value - this.Price) / this.Price
    
type SellsView(stocks:OwnedStock seq,prices:Dictionary<Ticker,StockQuote>) =
    
    member _.Sells: SellView seq =
        stocks
        |> Seq.collect (fun stock -> stock.State.BuyOrSell |> Seq.map (fun t -> {|stock = stock; buyOrSell = t|}))
        |> Seq.filter (fun t -> t.buyOrSell :? StockSold)
        |> Seq.filter (fun t -> t.buyOrSell.When > DateTimeOffset.UtcNow.AddDays(-60))
        |> Seq.groupBy (fun t -> t.stock.State.Ticker)
        |> Seq.map (fun (ticker, sells) -> {|ticker = ticker; latest = sells |> Seq.maxBy (fun t -> t.buyOrSell.When)|})
        |> Seq.map (fun t -> {
            Ticker = t.ticker.Value
            Date = t.latest.buyOrSell.When
            NumberOfShares = t.latest.buyOrSell.NumberOfShares
            Price = t.latest.buyOrSell.Price
            OlderThan30Days = t.latest.buyOrSell.When < DateTimeOffset.UtcNow.AddDays(-30)
            CurrentPrice =
                match prices.TryGetValue(t.ticker) with
                | true, q -> Nullable<decimal>(q.Price)
                | false, _ -> Nullable<decimal>()
        })
        
    
type Handler(accounts:IAccountStorage,brokerage:IBrokerage,marketHours:IMarketHours,storage:IPortfolioStorage) =
    
    let getLevel (position:PositionInstance) =
        match position.Profit |> abs with
        | profit when profit > 1000m -> 5
        | profit when profit > 500m -> 4
        | profit when profit > 100m -> 3
        | profit when profit > 50m -> 2
        | profit when profit > 10m -> 1
        | _ -> 0
        
    interface IApplicationService
    
    member _.Handle(request:ChainQuery) = task {
        let! stocks = storage.GetStocks request.UserId
        
        let links =
            stocks
            |> Seq.collect (fun stock -> stock.State.GetClosedPositions())
            |> Seq.sortByDescending (fun position -> position.Closed.Value)
            |> Seq.map (fun position -> {
                success=position.Profit > 0m
                ticker=position.Ticker
                level = position |> getLevel
                profit = position.Profit
            })
            
        let response = {links=links}
        
        return ServiceResponse<ChainView>(response)
    }
    
    member _.Handle(request:DailyPositionReportQuery) = task {
        
        let! user = accounts.GetUser request.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
        | Some user ->
            
            let! stock = storage.GetStock request.Ticker request.UserId
            
            match stock with
            | null -> return "Stock not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
            | _ ->
                
                let position = stock.State.GetPosition request.PositionId
                match position with
                | null -> return "Position not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
                | _ ->
                        
                    let start = position.Opened |> marketHours.GetMarketStartOfDayTimeInUtc
                    let ``end`` =
                        match position.Closed.HasValue with
                        | true -> position.Closed.Value |> marketHours.GetMarketEndOfDayTimeInUtc
                        | false -> DateTimeOffset.MinValue
                    
                    let! pricesResponse =
                        brokerage.GetPriceHistory
                            user.State
                            request.Ticker
                            PriceFrequency.Daily
                            start
                            ``end``
                    
                    match pricesResponse.Success with
                    | None -> return pricesResponse.Error.Value.Message |> ResponseUtils.failedTyped<DailyPositionReportView>
                    | Some prices ->
                        let profit, pct = PositionAnalysis.dailyPLAndGain prices position
                        
                        let response = {
                            DailyProfit = profit
                            DailyGainPct = pct
                            Ticker = request.Ticker.Value
                        }
                        
                        return ServiceResponse<DailyPositionReportView>(response)                   
    }
    
    member _.Handle (query:GapReportQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<GapReportView>
        | Some user ->
            
            let! priceResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    query.Frequency
                    DateTimeOffset.MinValue
                    DateTimeOffset.MinValue
            
            match priceResponse.Success with
            | None -> return priceResponse.Error.Value.Message |> ResponseUtils.failedTyped<GapReportView>
            | Some prices ->
                let gaps = detectGaps prices SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis
                let response = {gaps=gaps; ticker=query.Ticker.Value}
                return ServiceResponse<GapReportView>(response)
    }
    
    member _.HandleOutcomesReport userId (query:OutcomesReportQuery) = task {
        let! user = accounts.GetUser userId
        
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<OutcomesReportView>
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
                        
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            t
                            query.Frequency
                            startDate
                            endDate
                        |> Async.AwaitTask
                    
                    match priceResponse.Success with
                    | None -> return None
                    | Some prices ->
                        
                        let outcomes =
                            match query.Duration with
                            | OutcomesReportDuration.SingleBar ->  SingleBarPriceAnalysis.run prices
                            | OutcomesReportDuration.AllBars -> MultipleBarPriceAnalysis.run prices
                        
                        let tickerOutcome:TickerOutcomes = {outcomes=outcomes; ticker=t}
                        
                        let gapsView =
                            match query.IncludeGapAnalysis with
                            | true ->
                                let gaps = detectGaps prices SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis
                                Some {gaps=gaps; ticker=t.Value}
                            | false -> None
                            
                        let patterns = PatternDetection.generate prices
                        let tickerPatterns = {patterns = patterns; ticker = t}
                        
                        return Some (tickerOutcome, gapsView, tickerPatterns)
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
            
            let response = OutcomesReportView(evaluations, outcomes, cleanedGaps, patterns)
            
            return ServiceResponse<OutcomesReportView>(response)
    }
    
    member _.Handle (query:OutcomesReportForPositionsQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<OutcomesReportView>
        | Some user ->
            let! stocks = storage.GetStocks query.UserId
            
            let positions = stocks |> Seq.filter (fun stock -> stock.State.OpenPosition = null |> not) |> Seq.map (fun stock -> stock.State.OpenPosition)
            
            let! account = brokerage.GetAccount user.State
            let orders =
                match account.Success with
                | None -> Array.Empty<Order>()
                | Some account -> account.Orders
                
            let! tasks =
                positions
                |> Seq.map (fun position -> async {
                    let! priceResponse =
                        brokerage.GetPriceHistory
                            user.State
                            position.Ticker
                            PriceFrequency.Daily
                            position.Opened
                            DateTimeOffset.UtcNow
                        |> Async.AwaitTask
                    
                    match priceResponse.Success with
                    | None -> return None
                    | Some prices ->
                        // TODO: this is not nice, can we avoid it setting the price here?
                        match prices.Bars with
                        | [||] -> ()
                        | _ -> position.SetPrice prices.Last.Close
                        
                        let outcomes = PositionAnalysis.generate position prices orders
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
            
            let response = OutcomesReportView(evaluations, outcomes, [], patterns)
            
            return ServiceResponse<OutcomesReportView>(response)
    }
    
    member _.Handle (query:PercentChangeStatisticsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<PercentChangeStatisticsView>
        | Some user ->
            let! pricesResponse =
                brokerage.GetPriceHistory
                    user.State
                    query.Ticker
                    query.Frequency
                    DateTimeOffset.MinValue
                    DateTimeOffset.MinValue
            
            match pricesResponse.Success with
            | None -> return pricesResponse.Error.Value.Message |> ResponseUtils.failedTyped<PercentChangeStatisticsView>
            | Some prices ->
                let recent = 30 |> prices.LatestOrAll |> PercentChangeAnalysis.calculateForPriceBars
                let allTime = prices |> PercentChangeAnalysis.calculateForPriceBars
                let response = {Ticker=query.Ticker.Value; Recent=recent; AllTime=allTime}
                return ServiceResponse<PercentChangeStatisticsView>(response)
    }
    
    member _.Handle (query:SellsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | None -> return "User not found" |> ResponseUtils.failedTyped<SellsView>
        | Some user ->
            let! stocks = storage.GetStocks query.UserId
            
            let! priceResult =
                brokerage.GetQuotes
                    user.State
                    (stocks |> Seq.map (fun stock -> stock.State.Ticker))
            
            let prices =
                match priceResult.Success with
                | None -> Dictionary<Ticker,StockQuote>()
                | Some prices -> prices
                
            let response = SellsView(stocks, prices)
            return ServiceResponse<SellsView>(response)
    }