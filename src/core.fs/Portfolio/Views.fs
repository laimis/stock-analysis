namespace core.fs.Portfolio

open System
open System.Collections.Generic
open Microsoft.FSharp.Collections
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Stocks
open core.Stocks.Services.Trading
open core.fs

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
            
            let gains = ChartDataPointContainer<decimal>(label, DataPointChartType.bar, annotation)
          
            let min, max =
                transactions
                |> Seq.fold (fun (min,max) transaction ->
                    let value = valueFunc transaction
                    let min = if value < min then value else min
                    let max = if value > max then value else max
                    (min,max)
                ) (Decimal.MaxValue, Decimal.MinValue)
            ()
        
            let min = Math.Floor(min);
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
            
        let generateTrends (windowSize:int) (trades:PositionInstance array) =
            
            let trends = List<ChartDataPointContainer<decimal>>();
            
            let zeroLineAnnotationHorizontal = ChartAnnotationLine(0, "Zero", ChartAnnotationLineType.horizontal);
            let zeroLineAnnotationVertical = ChartAnnotationLine(0, "Zero", ChartAnnotationLineType.vertical);
            let oneLineAnnotationHorizontal = ChartAnnotationLine(1, "One", ChartAnnotationLineType.horizontal);
            
            // go over each closed transaction and calculate number of wins for each window
            let profits = ChartDataPointContainer<decimal>("Profits", DataPointChartType.line, zeroLineAnnotationHorizontal)
            let equityCurve = ChartDataPointContainer<decimal>("Equity Curve", DataPointChartType.line, zeroLineAnnotationHorizontal);
            let wins = ChartDataPointContainer<decimal>("Win %", DataPointChartType.line);
            let avgWinPct = ChartDataPointContainer<decimal>("Avg Win %", DataPointChartType.line);
            let avgLossPct = ChartDataPointContainer<decimal>("Avg Loss %", DataPointChartType.line);
            let ev = ChartDataPointContainer<decimal>("EV", DataPointChartType.line, zeroLineAnnotationHorizontal);
            let avgWinAmount = ChartDataPointContainer<decimal>("Avg Win $", DataPointChartType.line);
            let avgLossAmount = ChartDataPointContainer<decimal>("Avg Loss $", DataPointChartType.line);
            let gainPctRatio = ChartDataPointContainer<decimal>("% Ratio", DataPointChartType.line, oneLineAnnotationHorizontal);
            let profitRatio = ChartDataPointContainer<decimal>("$ Ratio", DataPointChartType.line, oneLineAnnotationHorizontal);
            let rrRatio = ChartDataPointContainer<decimal>("RR Ratio", DataPointChartType.line, oneLineAnnotationHorizontal);
            let maxWin = ChartDataPointContainer<decimal>("Max Win $", DataPointChartType.line);
            let maxLoss = ChartDataPointContainer<decimal>("Max Loss $", DataPointChartType.line);
            let rrSum = ChartDataPointContainer<decimal>("RR Sum", DataPointChartType.line);
            let invested = ChartDataPointContainer<decimal>("Invested", DataPointChartType.line, zeroLineAnnotationHorizontal);
            
            let days =
                match trades with
                | [||] -> -1
                | _ ->
                    let firstDate = trades[0].Closed.Value.Date
                    let lastDate = trades[trades.Length-1].Closed.Value.Date
                    (lastDate - firstDate).TotalDays |> int
            
            let mutable equity = 0m;
            
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
                
                // calculate equity curve
                equity <- equity + (trades |> Seq.filter (fun t -> t.Closed.Value.Date = start) |> Seq.sumBy (fun t -> t.Profit))
                
                equityCurve.Add(start, equity)
            )
            
            // non windowed stats
            let mutable aGrades = 0;
            let mutable bGrades = 0;
            let mutable cGrades = 0;
            let mutable minCloseDate = DateTimeOffset.MaxValue;
            let mutable maxCloseDate = DateTimeOffset.MinValue;
            let positionsClosedByDate = Dictionary<DateTimeOffset, int>();
            let positionsClosedByDateContainer = ChartDataPointContainer<decimal>("Positions Closed", DataPointChartType.bar);
            let positionsOpenedByDate = Dictionary<DateTimeOffset, int>();
            let positionsOpenedByDateContainer = ChartDataPointContainer<decimal>("Positions Opened", DataPointChartType.bar);
            
            trades
                |> Seq.iter ( fun position ->
                    if position.Grade = "A" then
                        aGrades <- aGrades + 1
                    else if position.Grade = "B" then
                        bGrades <- bGrades + 1
                    else if position.Grade = "C" then
                        cGrades <- cGrades + 1
                    else
                        ()
                        
                    if position.Closed.Value < minCloseDate then
                        minCloseDate <- position.Closed.Value
                    else
                        ()
                        
                    if position.Closed.Value > maxCloseDate then
                        maxCloseDate <- position.Closed.Value
                    else
                        ()
                        
                    if positionsClosedByDate.ContainsKey(position.Closed.Value.Date) then
                        positionsClosedByDate[position.Closed.Value.Date] <- positionsClosedByDate[position.Closed.Value.Date] + 1
                    else
                        positionsClosedByDate[position.Closed.Value.Date] <- 1
                        
                    if positionsOpenedByDate.ContainsKey(position.Opened.Value.Date) then
                        positionsOpenedByDate[position.Opened.Value.Date] <- positionsOpenedByDate[position.Opened.Value.Date] + 1
                    else
                        positionsOpenedByDate[position.Opened.Value.Date] <- 1
                )
            
            
            // for the range that the positions are in, calculate the number of buys for each date
            let days = (maxCloseDate - minCloseDate).TotalDays |> int
            
            for d in [0 .. days] do
                let key = minCloseDate.AddDays(float d).Date
                let positionsClosed = if positionsClosedByDate.ContainsKey(key) then positionsClosedByDate[key] else 0
                let positionsOpened = if positionsOpenedByDate.ContainsKey(key) then positionsOpenedByDate[key] else 0
                positionsClosedByDateContainer.Add(key, positionsClosed)
                positionsOpenedByDateContainer.Add(key, positionsOpened)
            
            let gradeContainer = ChartDataPointContainer<decimal>("Grade", DataPointChartType.bar);
            gradeContainer.Add("A", aGrades);
            gradeContainer.Add("B", bGrades);
            gradeContainer.Add("C", cGrades);
            
            
            let gainDistribution =
                let label = "Gain Distribution"
                match trades.Length with
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.bar)
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
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.bar)
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
                | 0 -> ChartDataPointContainer<decimal>(label, DataPointChartType.bar)
                | _ ->
                    generateOutcomeHistogram
                        label
                        trades
                        (fun p -> p.GainPct)
                        40
                        true
                        zeroLineAnnotationVertical
                        
            trends.Add(profits)
            trends.Add(equityCurve)
            // trends.Add(gradeContainer)
            // trends.Add(gainDistribution)
            // trends.Add(gainPctDistribution)
            // trends.Add(rrDistribution)
            // trends.Add(wins)
            // trends.Add(avgWinPct)
            // trends.Add(avgLossPct)
            // trends.Add(ev)
            // trends.Add(avgWinAmount)
            // trends.Add(avgLossAmount)
            // trends.Add(gainPctRatio)
            // trends.Add(profitRatio)
            // trends.Add(rrRatio)
            // trends.Add(rrSum)
            // trends.Add(invested)
            // trends.Add(maxWin)
            // trends.Add(maxLoss)
            // trends.Add(positionsOpenedByDateContainer)
            // trends.Add(positionsClosedByDateContainer)
            
            trends
            
        let generateTrendsForAtMost (numberOfTrades:int) (trades:PositionInstance array) =
            match trades.Length with
            | tradeLength when tradeLength >= numberOfTrades -> generateTrends 5 trades[0..numberOfTrades-1]
            | _ -> List<ChartDataPointContainer<decimal>>()
            
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
        member _.RecentClosedPositions = closedPositions |> Array.take recentLengthToTake
        member this.Recent = TradingPerformance.Create(this.RecentClosedPositions);
        member _.Overall = TradingPerformance.Create(closedPositions);
        member _.TrendsAll = generateTrends recentLengthToTake closedPositions;
        member _.TrendsLast20 = closedPositions |> generateTrendsForAtMost 20
        member _.TrendsLast50 = closedPositions |> generateTrendsForAtMost 50
        member _.TrendsLast100 = closedPositions |> generateTrendsForAtMost 100
        
        member this.TrendsTwoMonths =
            this.ClosedPositions
            |> timeBasedSlice (DateTimeOffset.Now.AddMonths(-2))
            |> generateTrends recentLengthToTake
        
        member _.TrendsYTD =
            closedPositions
            |> timeBasedSlice (DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero))
            |> generateTrends recentLengthToTake
        
        member _.TrendsOneYear = 
            closedPositions
            |> timeBasedSlice (DateTimeOffset.Now.AddYears(-1))
            |> generateTrends recentLengthToTake
            
type PortfolioView =
    {
        OpenStockCount: int
        OpenOptionCount: int
        OpenCryptoCount: int
    }


type TradingEntriesView =
    {
        current: PositionInstance array
        past: PositionInstance array
        performance: TradingPerformanceContainerView
        violations: StockViolationView array
        strategyPerformance: TradingStrategyPerformance array
        cashBalance: Nullable<decimal>
        brokerageOrders: Order array
    }
    
type TransactionGroup(name:string,transactions:Transaction seq) =
    member _.Name = name
    member _.Transactions = transactions
    member _.Sum = transactions |> Seq.sumBy (fun t -> t.Amount)
    

type TransactionsView(transactions:Transaction seq, groupBy:string, tickers:string seq) =
    
    let groupByValue (groupBy:string) (t:Transaction) =
        match groupBy with
        | "ticker" -> t.Ticker
        | "week" -> t.DateAsDate.AddDays(- float t.DateAsDate.DayOfWeek+1.0).ToString("MMMM dd, yyyy")
        | _ -> t.DateAsDate.ToString("MMMM, yyyy")
        
    let ordered groupBy (transactions:Transaction seq) =
        match groupBy with
        | "ticker" -> transactions |> Seq.sortBy (fun t -> t.Ticker)
        | _ -> transactions |> Seq.sortByDescending (fun t -> t.DateAsDate)
    
    member _.Transactions = ordered groupBy transactions
    member _.Tickers = tickers
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