namespace core.fs.Reports

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.Stocks
open core.Shared.Adapters.Storage
open core.Stocks
open core.Stocks.Services.Analysis
open core.fs

type ChainQuery =
    {
        UserId: Guid
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
    
type DailyOutcomeScoreReportQuery =
    {
        UserId: Guid
        Start: string
        End: string
        Ticker: Ticker
    }
    
type DailyOutcomeScoreReportView =
    {
        Ticker: Ticker
        DailyScores: DateScorePair seq
    }
    
type DailyPositionReportQuery =
    {
        UserId: Guid
        Ticker: string
        PositionId: int
    }
    
type DailyPositionReportView =
    {
        DailyProfit: DateScorePair seq
        DailyGainPct: DateScorePair seq
        Ticker: string
    }
    
type GapReportQuery =
    {
        UserId: Guid
        Ticker: string
        Frequency: PriceFrequency
    }
    
type GapReportView =
    {
        gaps: Gap seq
        ticker: string
    }
    
type OutcomesReportDuration =
    | SingleBar = 0
    | AllBars = 1

type OutcomesReportQuery =
    {
        [<Required>]
        Tickers: string[]
        UserId: Guid
        [<Required>]
        Duration: OutcomesReportDuration
        [<Required>]
        Frequency: PriceFrequency
        IncludeGapAnalysis: bool
        StartDate: string
        EndDate: string
    }
    
    static member WithUserId (userId:Guid) (query:OutcomesReportQuery) =
        {query with UserId=userId}
    
type OutcomesReportForPositionsQuery =
    {
        UserId: Guid
    }

type OutcomesReportViewTickerCountPair =
    {
        Ticker: string
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
        |> AnalysisOutcomeEvaluationScoringHelper.GenerateTickerCounts
        |> Seq.map (fun kv -> {Ticker=kv.Key; Count=kv.Value})
        |> Seq.sortByDescending (fun pair -> pair.Count)
    
    member _.EvaluationSummary: OutcomesReportViewEvaluationCountPair seq =
        evaluations
        |> AnalysisOutcomeEvaluationScoringHelper.GenerateEvaluationCounts
        |> Seq.map (fun kv -> (kv, evaluations |> Seq.find (fun e -> e.name = kv.Key)))
        |> Seq.sortBy (fun (kp, evaluation) -> (evaluation.``type``, kp.Value* -1))
        |> Seq.map (fun (kp, evaluation) -> {
            Evaluation=kp.Key
            Type=evaluation.``type``
            Count=kp.Value
        })
        
type PercentChangeStatisticsQuery =
    {
        Frequency: PriceFrequency
        Ticker: string
        UserId: Guid
    }
    
type PercentChangeStatisticsView =
    {
        Ticker: string
        Recent: DistributionStatistics
        AllTime: DistributionStatistics
    }
    
type SellsQuery =
    {
        UserId: Guid
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
    
type SellsView(stocks:OwnedStock seq,prices:Dictionary<string,StockQuote>) =
    
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
    
    member _.Handle(request:DailyOutcomeScoreReportQuery) = task {
        
        let! user = accounts.GetUser request.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<DailyOutcomeScoreReportView>
        | _ ->
            
            let start = request.Start |> DateTimeOffset.Parse |> marketHours.GetMarketStartOfDayTimeInUtc
            let ``end`` =
                match request.End with
                | null -> DateTimeOffset.MinValue
                | _ -> request.End |> DateTimeOffset.Parse |> marketHours.GetMarketEndOfDayTimeInUtc
            
            let! pricesResponse = brokerage.GetPriceHistory(
                state=user.State, ticker=request.Ticker, frequency=PriceFrequency.Daily,
                start=start.AddDays(-365), // going back a bit to have enough data for "relative" stats
                ``end``=``end``)
            
            match pricesResponse.IsOk with
            | false ->
                return pricesResponse.Error.Message |> ResponseUtils.failedTyped<DailyOutcomeScoreReportView>
            | true ->
                
                let scoresList = SingleBarDailyScoring.Generate(pricesResponse.Success, start, request.Ticker)
                let response = {Ticker=request.Ticker; DailyScores=scoresList}
                return ServiceResponse<DailyOutcomeScoreReportView>(response)
    }
    
    member _.Handle(request:DailyPositionReportQuery) = task {
        
        let! user = accounts.GetUser request.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
        | _ ->
            
            let! stock = storage.GetStock(ticker=request.Ticker, userId=request.UserId)
            
            match stock with
            | null -> return "Stock not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
            | _ ->
                
                let position = stock.State.GetPosition request.PositionId
                match position with
                | null -> return "Position not found" |> ResponseUtils.failedTyped<DailyPositionReportView>
                | _ ->
                        
                    let start = position.Opened.Value |> marketHours.GetMarketStartOfDayTimeInUtc
                    let ``end`` =
                        match position.Closed.HasValue with
                        | true -> position.Closed.Value |> marketHours.GetMarketEndOfDayTimeInUtc
                        | false -> DateTimeOffset.MinValue
                    
                    let! pricesResponse = brokerage.GetPriceHistory(
                        state=user.State, ticker=request.Ticker, frequency=PriceFrequency.Daily,
                        start=start, ``end``=``end``)
                    
                    match pricesResponse.IsOk with
                    | false ->
                        return pricesResponse.Error.Message |> ResponseUtils.failedTyped<DailyPositionReportView>
                    | true ->
                        let plAndGain = PositionDailyPLAndGain.Generate(
                                pricesResponse.Success,
                                position
                            )
                        
                        let profit, pct = plAndGain.ToTuple()
                        
                        let response = {
                            DailyProfit = profit
                            DailyGainPct = pct
                            Ticker = request.Ticker
                        }
                        
                        return ServiceResponse<DailyPositionReportView>(response)                   
    }
    
    member _.Handle (query:GapReportQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<GapReportView>
        | _ ->
            
            let! priceResponse = brokerage.GetPriceHistory(
                state=user.State, ticker=query.Ticker, frequency=query.Frequency
            )
            
            match priceResponse.IsOk with
            | false ->
                return priceResponse.Error.Message |> ResponseUtils.failedTyped<GapReportView>
            | true ->
                let gaps = GapAnalysis.Generate(priceResponse.Success, numberOfBarsToAnalyze=60)
                let response = {gaps=gaps; ticker=query.Ticker}
                return ServiceResponse<GapReportView>(response)
    }
    
    member _.Handle (query:OutcomesReportQuery) = task {
        let! user = accounts.GetUser query.UserId
        
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<OutcomesReportView>
        | _ ->
            
            let! tasks =
                query.Tickers
                |> Seq.map( fun t -> async {
                    let startDate =
                        match query.StartDate with
                        | null -> DateTimeOffset.MinValue
                        | _ -> marketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset.Parse(query.StartDate))
                    
                    let endDate =
                        match query.EndDate with
                        | null -> DateTimeOffset.MinValue
                        | _ -> marketHours.GetMarketEndOfDayTimeInUtc(DateTimeOffset.Parse(query.EndDate))
                        
                    let! priceResponse =
                        brokerage.GetPriceHistory(
                            user.State, t, query.Frequency, startDate, endDate
                        )
                        |> Async.AwaitTask
                    
                    match priceResponse.IsOk with
                    | false -> return None
                    | true ->
                        
                        let outcomes =
                            match query.Duration with
                            | OutcomesReportDuration.SingleBar ->  SingleBarAnalysisRunner.Run(priceResponse.Success)
                            | OutcomesReportDuration.AllBars ->
                                let lastPrice = priceResponse.Success[priceResponse.Success.Length - 1].Close
                                MultipleBarPriceAnalysis.Run(lastPrice, priceResponse.Success)
                            | _ -> failwith "Unexpected duration"
                        
                        let tickerOutcome:TickerOutcomes = TickerOutcomes(outcomes, t)
                        
                        let gapsView =
                            match query.IncludeGapAnalysis with
                            | true ->
                                let gaps = GapAnalysis.Generate(priceResponse.Success, numberOfBarsToAnalyze=60)
                                Some {gaps=gaps; ticker=t}
                            | false -> None
                            
                        let tickerPatterns = TickerPatterns(PatternDetection.Generate(priceResponse.Success), t)
                        
                        return Some (tickerOutcome, gapsView, tickerPatterns)
                    }
                )
                |> Async.Parallel
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
                | OutcomesReportDuration.SingleBar -> SingleBarAnalysisOutcomeEvaluation.Evaluate(outcomes)
                | OutcomesReportDuration.AllBars -> MultipleBarAnalysisOutcomeEvaluation.Evaluate(outcomes)
                | _ -> failwith "Unexpected duration"
            
            let response = OutcomesReportView(evaluations, outcomes, cleanedGaps, patterns)
            
            return ServiceResponse<OutcomesReportView>(response)
    }
    
    member _.Handle (query:OutcomesReportForPositionsQuery) = task {
        
        let! user = accounts.GetUser query.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<OutcomesReportView>
        | _ ->
            let! stocks = storage.GetStocks query.UserId
            
            let positions = stocks |> Seq.filter (fun stock -> stock.State.OpenPosition = null |> not) |> Seq.map (fun stock -> stock.State.OpenPosition)
            
            let! orderResponse = brokerage.GetAccount user.State
            let orders =
                match orderResponse.IsOk with
                | false -> Array.Empty<Order>()
                | true -> orderResponse.Success.Orders
                
            let! tasks =
                positions
                |> Seq.map (fun position -> async {
                    let! priceResponse =
                        brokerage.GetPriceHistory(
                            user.State, position.Ticker, PriceFrequency.Daily,
                            position.Opened.Value, DateTimeOffset.UtcNow
                        )
                        |> Async.AwaitTask
                    
                    match priceResponse.IsOk with
                    | false -> return None
                    | true ->
                        
                        // TODO: this is not nice, can we avoid it setting the price here?
                        match priceResponse.Success.Length > 0 with
                        | true -> position.SetPrice priceResponse.Success[priceResponse.Success.Length - 1].Close
                        | false -> ()
                        
                        let outcomes = PositionAnalysis.Generate(position, priceResponse.Success, orders)
                        let tickerOutcome:TickerOutcomes = TickerOutcomes(outcomes, position.Ticker)
                        let tickerPatterns = TickerPatterns(PatternDetection.Generate(priceResponse.Success), position.Ticker)
                        
                        return Some (tickerOutcome, tickerPatterns)
                    }
                )
                |> Async.Parallel
                |> Async.StartAsTask
                
            let outcomes, patterns =
                tasks
                |> Seq.choose id
                |> Seq.fold (fun (outcomes, patterns) (outcome, pattern) ->
                    let newOutcomes = outcomes @ [outcome]
                    let newPatterns = patterns @ [pattern]
                    (newOutcomes, newPatterns)
                ) ([], [])
                
            let evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(outcomes)
            
            let response = OutcomesReportView(evaluations, outcomes, [], patterns)
            
            return ServiceResponse<OutcomesReportView>(response)
    }
    
    member _.Handle (query:PercentChangeStatisticsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<PercentChangeStatisticsView>
        | _ ->
            let! pricesResponse = brokerage.GetPriceHistory(
                    user.State,
                    query.Ticker,
                    query.Frequency)
            
            match pricesResponse.IsOk with
            | false -> return pricesResponse.Error.Message |> ResponseUtils.failedTyped<PercentChangeStatisticsView>
            | true ->
                let toSkip =
                    match pricesResponse.Success.Length > 30 with
                    | true -> pricesResponse.Success.Length - 30
                    | false -> 0
                
                let recent = NumberAnalysis.PercentChanges(pricesResponse.Success |> Seq.skip toSkip |> Seq.map (fun p -> p.Close) |> Seq.toArray)
                let allTime = NumberAnalysis.PercentChanges(pricesResponse.Success |> Seq.map (fun p -> p.Close) |> Seq.toArray)
                let response = {Ticker=query.Ticker; Recent=recent; AllTime=allTime}
                return ServiceResponse<PercentChangeStatisticsView>(response)
    }
    
    member _.Handle (query:SellsQuery) = task {
        let! user = accounts.GetUser query.UserId
        match user with
        | null -> return "User not found" |> ResponseUtils.failedTyped<SellsView>
        | _ ->
            let! stocks = storage.GetStocks query.UserId
            
            let! priceResult = brokerage.GetQuotes(user.State, stocks |> Seq.map (fun stock -> stock.State.Ticker.Value))
            
            let prices =
                match priceResult.IsOk with
                | false -> Dictionary<string,StockQuote>()
                | true -> priceResult.Success
                
            let response = SellsView(stocks, prices)
            return ServiceResponse<SellsView>(response)
    }