namespace core.fs.Portfolio

open System
open System.Collections.Generic
open Microsoft.FSharp.Collections
open core.Shared
open core.Stocks
open core.fs.Services.Trading
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage

// TODO: this view class is very busy, doing all kinds of stuff. Maybe a service
// should do this and this type would just contain data...
type TradingPerformanceContainerView(inputPositions:PositionInstance array) =
     
        // feels a bit hacky, but the expectation is that the positions will always be sorted
        // from the most recent to the oldest. And for performance trends we want window by
        // date from oldest to most recent
        let closedPositions = inputPositions |> Array.sortBy (fun p -> p.Closed.Value)
        
        let defaultWindowSize = 20
        
        let recentLengthToTake =
            match closedPositions.Length > defaultWindowSize with
            | true -> defaultWindowSize
            | false -> closedPositions.Length
            
        
        let generateOutcomeHistogram (label:string) transactions valueFunc (buckets:int) symmetric annotation =
            
            let gains = ChartDataPointContainer<decimal>(label, DataPointChartType.Column, annotation)
          
            let min, max =
                transactions
                |> Seq.fold (fun (min,max) transaction ->
                    let value = valueFunc transaction
                    let min = if value < min then value else min
                    let max = if value > max then value else max
                    (min,max)
                ) (Decimal.MaxValue, Decimal.MinValue)
            ()
        
            let min = Math.Floor(min)
            let max = Math.Ceiling(max)
            
            let min,max =
                match symmetric with
                | true ->
                    let absMax = Math.Max(Math.Abs(min), Math.Abs(max))
                    (-absMax, absMax)
                | false -> (min,max)
        
            let step = (max - min) / (buckets |> decimal)
            let step =
                match step with
                | x when x < 1.0m -> Math.Round(step, 4)
                | _ -> Math.Round(step, 0)
                
            [0..buckets]
            |> Seq.iter (fun i ->
                let lower = min + (step * decimal i)
                let upper = min + (step * (decimal i + 1.0m))
                let count =
                    transactions
                    |> Seq.filter (fun t ->
                        let value = valueFunc t
                        value >= lower && value < upper
                    )
                    |> Seq.length
                gains.Add(lower.ToString(), decimal count)
            )
            gains
            
        let generateTrends (trades:PositionInstance array) =
            
            let zeroLineAnnotationHorizontal = ChartAnnotationLine(0, ChartAnnotationLineType.Horizontal) |> Option.Some
            let zeroLineAnnotationVertical = ChartAnnotationLine(0, ChartAnnotationLineType.Vertical) |> Option.Some
            let oneLineAnnotationHorizontal = ChartAnnotationLine(1, ChartAnnotationLineType.Horizontal) |> Option.Some
            
            // go over each closed transaction and calculate number of wins for each window
            let profits = ChartDataPointContainer<decimal>("Profits", DataPointChartType.Line, zeroLineAnnotationHorizontal)
            let equityCurve = ChartDataPointContainer<decimal>("Equity Curve", DataPointChartType.Line, zeroLineAnnotationHorizontal)
            let wins = ChartDataPointContainer<decimal>("Win %", DataPointChartType.Line, ChartAnnotationLine(0.4m, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let avgWinPct = ChartDataPointContainer<decimal>("Avg Win %", DataPointChartType.Line, ChartAnnotationLine(0.12m, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let avgLossPct = ChartDataPointContainer<decimal>("Avg Loss %", DataPointChartType.Line, ChartAnnotationLine(-0.07m, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let ev = ChartDataPointContainer<decimal>("EV", DataPointChartType.Line, ChartAnnotationLine(40, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let avgWinAmount = ChartDataPointContainer<decimal>("Avg Win $", DataPointChartType.Line, ChartAnnotationLine(60, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let avgLossAmount = ChartDataPointContainer<decimal>("Avg Loss $", DataPointChartType.Line, ChartAnnotationLine(-30, ChartAnnotationLineType.Horizontal) |> Option.Some)
            let gainPctRatio = ChartDataPointContainer<decimal>("% Ratio", DataPointChartType.Line, oneLineAnnotationHorizontal)
            let profitRatio = ChartDataPointContainer<decimal>("$ Ratio", DataPointChartType.Line, oneLineAnnotationHorizontal)
            let rrRatio = ChartDataPointContainer<decimal>("RR Ratio", DataPointChartType.Line, oneLineAnnotationHorizontal)
            let maxWin = ChartDataPointContainer<decimal>("Max Win $", DataPointChartType.Line)
            let maxLoss = ChartDataPointContainer<decimal>("Max Loss $", DataPointChartType.Line)
            let rrSum = ChartDataPointContainer<decimal>("RR Sum", DataPointChartType.Line)
            let invested = ChartDataPointContainer<decimal>("Invested", DataPointChartType.Line)
            let tradeCount = ChartDataPointContainer<decimal>("Trade Count", DataPointChartType.Line)
            let positionsClosedByDateContainer = ChartDataPointContainer<decimal>("Positions Closed", DataPointChartType.Column)
            let positionsOpenedByDateContainer = ChartDataPointContainer<decimal>("Positions Opened", DataPointChartType.Column)
            
            let days =
                match trades with
                | [||] -> -1
                | _ ->
                    let firstDate = trades[0].Closed.Value.Date
                    let lastDate = trades[trades.Length-1].Closed.Value.Date
                    (lastDate - firstDate).TotalDays |> int
            
            let mutable equity = 0m
            
            [0..days]
            |> Seq.iter (fun i ->
                let firstDate = trades[0].Closed.Value.Date
                let start = firstDate.AddDays(i)
                let ``end`` = firstDate.AddDays(float i+20.0)
                
                let perfView = trades |> Seq.filter (fun t -> t.Closed.Value.Date >= start && t.Closed.Value.Date < ``end``) |> TradingPerformance.Create
                
                profits.Add(start, perfView.Profit)
                wins.Add(start, perfView.WinPct)
                avgWinPct.Add(start, perfView.WinAvgReturnPct)
                avgLossPct.Add(start, perfView.LossAvgReturnPct)
                ev.Add(start, perfView.EV)
                avgWinAmount.Add(start, perfView.AvgWinAmount)
                avgLossAmount.Add(start, perfView.AvgLossAmount)
                gainPctRatio.Add(start, perfView.ReturnPctRatio)
                profitRatio.Add(start, perfView.ProfitRatio)
                rrRatio.Add(start, perfView.rrRatio)
                maxWin.Add(start, perfView.MaxWinAmount)
                maxLoss.Add(start, perfView.MaxLossAmount)
                rrSum.Add(start, perfView.rrSum)
                invested.Add(start, perfView.TotalCost)
                tradeCount.Add(start, perfView.NumberOfTrades)
                
                // number of positions opened on start day
                let numberOfPositionsOpened = trades |> Seq.filter (fun t -> t.Opened.Date = start) |> Seq.length
                positionsOpenedByDateContainer.Add(start, decimal numberOfPositionsOpened)
                
                // number of positions closed on start day
                let numberOfPositionsClosed = trades |> Seq.filter (fun t -> t.Closed.Value.Date = start) |> Seq.length
                positionsClosedByDateContainer.Add(start, decimal numberOfPositionsClosed)
                
                // calculate equity curve
                equity <- equity + (trades |> Seq.filter (fun t -> t.Closed.Value.Date = start) |> Seq.sumBy (fun t -> t.Profit))
                
                equityCurve.Add(start, equity)
            )
            
            let aGrades, bGrades, cGrades =
                trades
                |> Seq.fold ( fun (a, b, c) position ->
                    match position.Grade.HasValue with
                    | false -> (a, b, c)
                    | _ ->
                        match position.Grade.Value.Value with
                        | "A" -> (a+1, b, c)
                        | "B" -> (a, b+1, c)
                        | "C" -> (a, b, c+1)
                        | _ -> (a, b, c)
                        
                    ) (0, 0, 0)
            
            let gradeContainer = ChartDataPointContainer<decimal>("Grade", DataPointChartType.Column)
            gradeContainer.Add("A", aGrades)
            gradeContainer.Add("B", bGrades)
            gradeContainer.Add("C", cGrades)
            
            let gainDistribution =
                let label = "Gain Distribution"
                match trades.Length with
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
                | _ ->
                    generateOutcomeHistogram
                        label
                        trades
                        (fun p -> p.Profit)
                        20
                        true
                        zeroLineAnnotationVertical
            
            let rrDistribution =
                let label = "RR Distribution"
                match trades.Length with
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
                | _ ->
                    generateOutcomeHistogram
                        label
                        trades
                        (fun p -> p.RR)
                        10
                        true
                        zeroLineAnnotationVertical
            
            let gainPctDistribution =
                let label = "Gain % Distribution"
                match trades.Length with
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
                | _ ->
                    generateOutcomeHistogram
                        label
                        trades
                        (fun p -> p.GainPct)
                        40
                        true
                        zeroLineAnnotationVertical
            
            [
                profits
                equityCurve
                gradeContainer
                gainDistribution
                gainPctDistribution
                rrDistribution
                wins
                avgWinPct
                avgLossPct
                ev
                avgWinAmount
                avgLossAmount
                gainPctRatio
                profitRatio
                rrRatio
                rrSum
                invested
                maxWin
                maxLoss
                tradeCount
                positionsOpenedByDateContainer
                positionsClosedByDateContainer
            ]
            
        let getAtMost (numberOfTrades:int) (trades:PositionInstance array) =
            match trades.Length with
            | tradeLength when tradeLength >= numberOfTrades -> trades[trades.Length - numberOfTrades - 1..trades.Length-1]
            | _ -> Array.Empty<PositionInstance>()
            
        let timeBasedSlice cutOff (trades:PositionInstance array) =
            
            let firstTradeIndex =
                trades
                |> Array.indexed
                |> Array.filter (fun (_,trade) -> trade.Closed.Value >= cutOff)
                |> Array.tryHead
                
            let span =
                match firstTradeIndex with
                | Some (index,_) -> Span<PositionInstance>(trades, index, trades.Length - index)
                | None -> Span<PositionInstance>(trades, 0, 0)
                
            span.ToArray()
        
        member _.ClosedPositions = closedPositions
        member _.RecentClosedPositions = closedPositions |> Array.skip (closedPositions.Length - recentLengthToTake)
        
        member _.PerformanceAll = closedPositions |> TradingPerformance.Create
        member _.PerformanceLast20 = closedPositions |> getAtMost 20 |> TradingPerformance.Create
        member _.PerformanceLast50 = closedPositions |> getAtMost 50 |> TradingPerformance.Create
        member _.PerformanceLast100 = closedPositions |> getAtMost 100 |> TradingPerformance.Create
        member _.PerformanceTwoMonths =
            closedPositions
            |> timeBasedSlice (DateTimeOffset.UtcNow.AddMonths(-2))
            |> TradingPerformance.Create
            
        member _.PerformanceYTD =
            closedPositions
            |> timeBasedSlice (DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero))
            |> TradingPerformance.Create
            
        member _.PerformanceOneYear =
            closedPositions
            |> timeBasedSlice (DateTimeOffset.UtcNow.AddYears(-1))
            |> TradingPerformance.Create
            
        // TODO: very slow to calculate, commenting it out as I need to glance it rarely, but perhaps it's something
        // we can precalculate
        // member _.TrendsAll = closedPositions |> generateTrends
        member _.TrendsLast20 = closedPositions |> getAtMost 20 |> generateTrends
        member _.TrendsLast50 = closedPositions |> getAtMost 50 |> generateTrends
        member _.TrendsLast100 = closedPositions |> getAtMost 100 |> generateTrends
        
        member this.TrendsTwoMonths =
            this.ClosedPositions
            |> timeBasedSlice (DateTimeOffset.UtcNow.AddMonths(-2))
            |> generateTrends
        
        member _.TrendsYTD =
            closedPositions
            |> timeBasedSlice (DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero))
            |> generateTrends
        
        member _.TrendsOneYear = 
            closedPositions
            |> timeBasedSlice (DateTimeOffset.UtcNow.AddYears(-1))
            |> generateTrends
            
type PortfolioView =
    {
        OpenStockCount: int
        OpenOptionCount: int
        OpenCryptoCount: int
    }


type TradingEntriesView =
    {
        current: PositionInstance array
        violations: StockViolationView array
        cashBalance: decimal option
        brokerageOrders: Order array
        prices: Map<string, StockQuote>
    }
    
type PastTradingEntriesView =
    {
        past: PositionInstance array
        performance: TradingPerformanceContainerView
        strategyPerformance: TradingStrategyPerformance array
    }
    
type TransactionGroup(name:string,transactions:Transaction seq) =
    member _.Name = name
    member _.Transactions = transactions
    member _.Sum = transactions |> Seq.sumBy (fun t -> t.Amount)
    

type TransactionsView(transactions:Transaction seq, groupBy:string, tickers:Ticker array) =
    
    let groupByValue (groupBy:string) (t:Transaction) =
        match groupBy with
        | "ticker" -> t.Ticker.Value
        | "week" -> t.DateAsDate.AddDays(- float t.DateAsDate.DayOfWeek+1.0).ToString("MMMM dd, yyyy")
        | _ -> t.DateAsDate.ToString("MMMM, yyyy")
        
    let ordered groupBy (transactions:Transaction seq) =
        match groupBy with
        | "ticker" -> transactions |> Seq.sortBy (fun t -> t.Ticker)
        | _ -> transactions |> Seq.sortByDescending (fun t -> t.DateAsDate)
    
    member _.Transactions = ordered groupBy transactions
    member _.Tickers = tickers |> Array.map (fun t -> t.Value)
    member _.Grouped =
        match groupBy with
        | null  -> Seq.empty<TransactionGroup>
        | _ -> 
            let grouped =
                transactions
                |> ordered groupBy
                |> Seq.groupBy (groupByValue groupBy)
                |> Seq.map (fun (key,transactions) -> TransactionGroup(key,transactions))
            
            match groupBy with
            | "ticker" -> grouped |> Seq.sortByDescending (fun g -> g.Transactions |> Seq.sumBy (fun t -> t.Amount))
            | _ -> grouped
    
    member _.Credit = transactions |> Seq.sumBy (fun t -> t.Amount)
    member _.Debit = transactions |> Seq.sumBy (fun t -> t.Amount)
    
type TransactionSummaryView(start,``end``,openPositions,closedPositions,stockTransactions,optionTransactions,plStockTransactions,plOptionTransactions) =
        
        member _.Start = start
        member _.End = ``end``
        member _.OpenPositions = openPositions
        member _.ClosedPositions = closedPositions
        member _.StockTransactions = stockTransactions
        member _.OptionTransactions = optionTransactions
        member _.PLStockTransactions = plStockTransactions
        member _.PLOptionTransactions = plOptionTransactions
        
        member _.StockProfit = plStockTransactions |> Seq.sumBy (fun (t:Transaction) -> t.Amount)
        member _.OptionProfit = plOptionTransactions |> Seq.sumBy (fun (t:Transaction) -> t.Amount)