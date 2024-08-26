namespace core.fs.Portfolio

open System
open System.Collections.Generic
open Microsoft.FSharp.Collections
open core.Shared
open core.Stocks
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Reports
open core.fs.Services.Analysis
open core.fs.Services.Trading

open core.fs.Stocks


[<CustomEquality>]
[<CustomComparison>]
type StockViolationView =
    {
        CurrentPrice: decimal
        Message: string
        NumberOfShares: decimal
        PricePerShare: decimal
        Ticker: Ticker
        LocalPosition: StockPositionWithCalculations option
        PendingPosition: PendingStockPositionState option
    }
    
    override this.Equals(other) =
        match other with
        | :? StockViolationView as res -> res.Ticker = this.Ticker
        | _ -> false
    override this.GetHashCode() = this.Ticker.GetHashCode()
    
    interface IComparable with
        member this.CompareTo(other) =
            match other with
            | :? StockViolationView as res -> this.Ticker.Value.CompareTo(res.Ticker.Value)
            | _ -> -1
            
// TODO: this view class is very busy, doing all kinds of stuff. Maybe a service
// should do this and this type would just contain data...
type TradingPerformanceContainerView(inputPositions:StockPositionWithCalculations array) =
     
    // feels a bit hacky, but the expectation is that the positions will always be sorted
    // from the most recent to the oldest. And for performance trends we want window by
    // date from oldest to most recent
    let closedPositions = inputPositions |> Array.sortBy (_.Closed.Value)
    
    let roundedToPlaces (value:decimal) (places:int) = Math.Round(value, places)
    let rounded4 (value:decimal) = roundedToPlaces value 4
    let rounded2 (value:decimal) = roundedToPlaces value 2
    
    let generateHistogramDataContainer (label:string) trades valueFunc (buckets:int) symmetric annotation =
        
        trades
        |> Seq.map valueFunc
        |> Histogram.calculateFromSequence symmetric buckets
        |> Seq.fold (fun (container:ChartDataPointContainer<decimal>) i -> container.Add(i.value.ToString(), i.frequency)) (ChartDataPointContainer<decimal>(label, DataPointChartType.Column, annotation))
        
    let generateTrends name (trades:StockPositionWithCalculations array) =
        
        let zeroLineAnnotationHorizontal = ChartAnnotationLine(0, ChartAnnotationLineType.Horizontal) |> Option.Some
        let zeroLineAnnotationVertical = ChartAnnotationLine(0, ChartAnnotationLineType.Vertical) |> Option.Some
        let twoLineAnnotationHorizontal = ChartAnnotationLine(2, ChartAnnotationLineType.Horizontal) |> Option.Some
        let tradeInterval = 10
        
        // go over each closed transaction and calculate number of wins for each window
        let profits = ChartDataPointContainer<decimal>("Profits", DataPointChartType.Line, zeroLineAnnotationHorizontal)
        let equityCurve = ChartDataPointContainer<decimal>("Equity Curve", DataPointChartType.Line, zeroLineAnnotationHorizontal)
        let drawDowns = ChartDataPointContainer<decimal>("Draw downs", DataPointChartType.Line)
        let profitsFixedNumberOfTrades = ChartDataPointContainer<decimal>($"Profits (last {tradeInterval} trades)", DataPointChartType.Line, zeroLineAnnotationHorizontal)
        let wins = ChartDataPointContainer<decimal>("Win %", DataPointChartType.Line, ChartAnnotationLine(0.4m, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let avgWinPct = ChartDataPointContainer<decimal>("Avg Win %", DataPointChartType.Line, ChartAnnotationLine(0.12m, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let avgLossPct = ChartDataPointContainer<decimal>("Avg Loss %", DataPointChartType.Line, ChartAnnotationLine(-0.07m, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let ev = ChartDataPointContainer<decimal>("EV", DataPointChartType.Line, ChartAnnotationLine(40, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let avgWinAmount = ChartDataPointContainer<decimal>("Avg Win $", DataPointChartType.Line, ChartAnnotationLine(60, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let avgLossAmount = ChartDataPointContainer<decimal>("Avg Loss $", DataPointChartType.Line, ChartAnnotationLine(-30, ChartAnnotationLineType.Horizontal) |> Option.Some)
        let gainPctRatio = ChartDataPointContainer<decimal>("% Ratio", DataPointChartType.Line, twoLineAnnotationHorizontal)
        let profitRatio = ChartDataPointContainer<decimal>("$ Ratio", DataPointChartType.Line, twoLineAnnotationHorizontal)
        let rrRatio = ChartDataPointContainer<decimal>("RR Ratio", DataPointChartType.Line, twoLineAnnotationHorizontal)
        let maxWin = ChartDataPointContainer<decimal>("Max Win $", DataPointChartType.Line)
        let maxLoss = ChartDataPointContainer<decimal>("Max Loss $", DataPointChartType.Line)
        let rrSum = ChartDataPointContainer<decimal>("RR Sum", DataPointChartType.Line)
        let rrAverage = ChartDataPointContainer<decimal>("RR Average", DataPointChartType.Line, zeroLineAnnotationHorizontal)
        let rrMovingAverage = ChartDataPointContainer<decimal>($"RR Moving Average ({tradeInterval} trades)", DataPointChartType.Line, zeroLineAnnotationHorizontal)
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
        
        
        [0..days]
        |> Seq.fold (fun (equity, maxEquity, maxDrawdown) i ->
            let firstDate = trades[0].Closed.Value.Date
            let start = firstDate.AddDays(i)
            let ``end`` = firstDate.AddDays(float i+20.0)
            
            let perfView = trades |> Seq.filter (fun t -> t.Closed.Value.Date >= start && t.Closed.Value.Date < ``end``) |> TradingPerformance.Create name
            
            profits.Add(start, perfView.Profit)
            wins.Add(start, perfView.WinPct |> rounded4)
            avgWinPct.Add(start, perfView.WinAvgReturnPct |> rounded4)
            avgLossPct.Add(start, perfView.LossAvgReturnPct |> rounded4)
            ev.Add(start, perfView.EV |> rounded4)
            avgWinAmount.Add(start, perfView.AvgWinAmount |> rounded4)
            avgLossAmount.Add(start, perfView.AvgLossAmount |> rounded4)
            gainPctRatio.Add(start, perfView.ReturnPctRatio |> rounded4)
            profitRatio.Add(start, perfView.ProfitRatio |> rounded4)
            rrRatio.Add(start, perfView.rrRatio |> rounded4)
            maxWin.Add(start, perfView.MaxWinAmount |> rounded4)
            maxLoss.Add(start, perfView.MaxLossAmount |> rounded4)
            rrSum.Add(start, perfView.rrSum |> rounded4)
            rrAverage.Add(start, perfView.AverageRR |> rounded4)
            invested.Add(start, perfView.TotalCost |> rounded4)
            tradeCount.Add(start, perfView.NumberOfTrades)
            
            // number of positions opened on start day
            let numberOfPositionsOpened = trades |> Seq.filter (fun t -> t.Opened.Date = start) |> Seq.length
            positionsOpenedByDateContainer.Add(start, decimal numberOfPositionsOpened)
            
            // number of positions closed on start day
            let numberOfPositionsClosed = trades |> Seq.filter (fun t -> t.Closed.Value.Date = start) |> Seq.length
            positionsClosedByDateContainer.Add(start, decimal numberOfPositionsClosed)
            
            let newEquity = equity + (trades |> Seq.filter (fun t -> t.Closed.Value.Date = start) |> Seq.sumBy (_.Profit))
            
            // calculate equity curve
            equityCurve.Add(start, newEquity)
            
            let newMaxEquity = Math.Max(maxEquity, newEquity)
            
            let drawDown = newEquity - newMaxEquity
            
            let newMaxDrawdown = Math.Min(maxDrawdown, drawDown)
            
            drawDowns.Add(start, drawDown)
            
            (newEquity, newMaxEquity, newMaxDrawdown)
        ) (0m, 0m, 0m) |> ignore
        
        // moving average of RR using fixed number of trades instead of date
        trades
        |> Array.windowed tradeInterval
        |> Array.iter (fun window ->
            let date = window |> Array.last |> _.Closed.Value
            
            let avg = window |> Array.averageBy _.RR
            rrMovingAverage.Add(date,avg)
            
            let profit = window |> Array.sumBy _.Profit
            profitsFixedNumberOfTrades.Add(date, profit)
        )
        
        let profitVsDaysHeld =
            trades
            |> Seq.sortBy _.DaysHeld
            |> Seq.fold (fun (container:ChartDataPointContainer<decimal>) trade ->
                container.Add(trade.DaysHeld.ToString(), trade.Profit, trade.Ticker.Value)
            ) (ChartDataPointContainer<decimal>("Profit vs Days Held", DataPointChartType.Scatter, zeroLineAnnotationHorizontal))
        
        let aGrades, bGrades, cGrades =
            trades
            |> Seq.fold ( fun (a, b, c) position ->
                match position.Grade with
                | None -> (a, b, c)
                | Some grade ->
                    match grade.Value with
                    | "A" -> (a+1, b, c)
                    | "B" -> (a, b+1, c)
                    | "C" -> (a, b, c+1)
                    | _ -> (a, b, c)
                    
                ) (0, 0, 0)
        
        let gradeContainer = ChartDataPointContainer<decimal>("Grade", DataPointChartType.Column)
        gradeContainer.Add("A", aGrades) |> ignore
        gradeContainer.Add("B", bGrades) |> ignore
        gradeContainer.Add("C", cGrades) |> ignore
        
        let gainDistribution =
            let label = "Gain Distribution"
            match trades.Length with
            | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
            | _ ->
                generateHistogramDataContainer
                    label
                    trades
                    (_.Profit)
                    20
                    true
                    zeroLineAnnotationVertical
        
        let rrDistribution =
            let label = "RR Distribution"
            match trades.Length with
            | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
            | _ ->
                generateHistogramDataContainer
                    label
                    trades
                    (_.RR)
                    10
                    true
                    zeroLineAnnotationVertical
                    
        let gainPctDistribution =
            let label = "Gain % Distribution"
            match trades.Length with
            | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.Column)
            | _ ->
                generateHistogramDataContainer
                    label
                    trades
                    (_.GainPct)
                    40
                    true
                    zeroLineAnnotationVertical
                    
        let profitByIndex = ChartDataPointContainer<decimal>("Profits", DataPointChartType.Column)
        trades |> Seq.sortBy _.Profit |> Seq.iteri (fun i p -> profitByIndex.Add(i.ToString(), p.Profit) |> ignore)
        
        let rrByIndex = ChartDataPointContainer<decimal>("RR", DataPointChartType.Column)
        trades |> Seq.sortBy _.RR |> Seq.iteri (fun i p -> rrByIndex.Add(i.ToString(), p.RR) |> ignore)
        
        [
            equityCurve
            drawDowns
            profitVsDaysHeld
            profits
            profitsFixedNumberOfTrades
            gradeContainer
            gainDistribution
            gainPctDistribution
            rrDistribution
            profitByIndex
            rrByIndex
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
            rrAverage
            rrMovingAverage
            invested
            maxWin
            maxLoss
            tradeCount
            positionsOpenedByDateContainer
            positionsClosedByDateContainer
        ]
        
    let getAtMost (numberOfTrades:int) (trades:StockPositionWithCalculations array) =
        match trades.Length with
        | tradeLength when tradeLength >= numberOfTrades -> trades[trades.Length - numberOfTrades..]
        | _ -> Array.Empty<StockPositionWithCalculations>()
        
    let timeBasedSlice startDate endDate (trades:StockPositionWithCalculations array) =
        
        let matchingTrades =
            trades
            |> Array.indexed
            |> Array.filter (fun (_,trade) -> trade.Closed.Value >= startDate && trade.Closed.Value <= endDate)
            
        let firstIndex, lastIndex =
            match matchingTrades.Length with
            | 0 -> (None, None)
            | _ -> (matchingTrades[0] |> fst |> Some, matchingTrades[matchingTrades.Length-1] |> fst |> Some)
            
        let span =
            match firstIndex, lastIndex with
            | Some firstIndex, Some lastIndex -> Span<StockPositionWithCalculations>(trades, firstIndex, lastIndex-firstIndex+1)
            | _ -> Span<StockPositionWithCalculations>(trades, 0, 0)
            
        span.ToArray()

    let last20 = closedPositions |> getAtMost 20
    let last50 = closedPositions |> getAtMost 50
    let last100 = closedPositions |> getAtMost 100
    let last2Months = closedPositions |> timeBasedSlice (DateTimeOffset.UtcNow.AddMonths(-2)) DateTimeOffset.UtcNow
    let ytd = closedPositions |> timeBasedSlice (DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero)) DateTimeOffset.UtcNow
    let oneYear = closedPositions |> timeBasedSlice (DateTimeOffset.UtcNow.AddYears(-1)) DateTimeOffset.UtcNow
    
    let yearByYear =
        closedPositions
        |> Array.map (fun p -> p.Closed.Value.Year)
        |> Array.distinct
        |> Array.map (fun year ->
            let start = DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero)
            let ``end`` = DateTimeOffset(year, 12, 31, 0, 0, 0, TimeSpan.Zero)
            let trades = closedPositions |> timeBasedSlice start ``end``
            (year, trades)
        )
            
    let trends =
        let trends =
            [
                generateTrends "Last 20" last20
                generateTrends "Last 50" last50
                generateTrends "Last 100" last100
                generateTrends "Last 2 Months" last2Months
                generateTrends "YTD" ytd
                generateTrends "1 Year" oneYear
                yield! yearByYear |> Array.map (fun (year, trades) -> generateTrends $"%d{year}" trades)
                // generateTrends "All" closedPositions
            ]
        trends
        
    let performances =
        let performances =
            [
                last20 |> TradingPerformance.Create "Last 20"
                last50 |> TradingPerformance.Create "Last 50"
                last100 |> TradingPerformance.Create "Last 100"
                last2Months |> TradingPerformance.Create "Last 2 Months"
                ytd |> TradingPerformance.Create "YTD"
                oneYear |> TradingPerformance.Create "1 Year"
                yield! yearByYear |> Array.map (fun (year, trades) -> trades |> TradingPerformance.Create $"%d{year}")
                closedPositions |> TradingPerformance.Create "All"
            ]
        performances
        
    member _.Performances = performances    
    member _.Trends = trends
            
type PortfolioView =
    {
        OpenStockCount: int
        OpenOptionCount: int
        OpenCryptoCount: int
    }

type TradingEntriesView =
    {
        current: StockPositionWithCalculations array
        violations: StockViolationView array
        brokerageAccount: BrokerageAccount
        prices: Map<string, StockQuote>
        dailyBalances: AccountBalancesSnapshot seq
    }
    
type PastTradingEntriesView =
    {
        past: StockPositionWithCalculations array
    }
    
type PastTradingPerformanceView =
    {
        performance: TradingPerformanceContainerView
        strategyPerformance: TradingStrategyPerformance array
    }
    
type TransactionGroup(name:string,transactions:Transaction seq) =
    member _.Name = name
    member _.Transactions = transactions
    member _.Sum = transactions |> Seq.sumBy _.Amount
    

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
    
    member _.Transactions = ordered groupBy transactions
    member _.Tickers = tickers |> Array.map _.Value
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
    
type TransactionSummaryView(
    start,
    ``end``,
    openPositions:StockPositionWithCalculations list,
    closedPositions:StockPositionWithCalculations list,
    stockTransactions:PLTransaction list,
    optionTransactions:Transaction list,
    plStockTransactions:PLTransaction list,
    plOptionTransactions:Transaction list) =
        
        member _.Start = start
        member _.End = ``end``
        member _.OpenPositions = openPositions
        member _.ClosedPositions = closedPositions
        member _.StockTransactions = stockTransactions
        member _.OptionTransactions = optionTransactions
        member _.PLStockTransactions = plStockTransactions
        member _.PLOptionTransactions = plOptionTransactions
        
        member _.StockProfit = plStockTransactions |> Seq.sumBy (fun (t:PLTransaction) -> t.Profit)
        member _.OptionProfit = plOptionTransactions |> Seq.sumBy (fun (t:Transaction) -> t.Amount)
