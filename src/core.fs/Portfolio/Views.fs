namespace core.fs.Portfolio

open System
open Microsoft.FSharp.Collections
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Stocks
open core.Stocks.Services.Trading
open core.Stocks.View

type TradingPerformanceContainerView(closedPositions:PositionInstance array,numberOfPositions:int) =
        let recentLengthToTake =
            match closedPositions.Length > numberOfPositions with
            | true -> numberOfPositions
            | false -> closedPositions.Length
            
            
        let generateTrends (windowSize:int) (trades:PositionInstance array) =
            Array.Empty<ChartDataPointContainer<decimal>>()
            
        let generateTrendsForAtMost (numberOfTrades:int) (trades:PositionInstance array) =
            match trades.Length with
            | x when x >= numberOfTrades -> generateTrends 5 trades[trades.Length - numberOfTrades..numberOfTrades]
            | _ -> Array.Empty<ChartDataPointContainer<decimal>>()
            
        let timeBasedSlice cutOff (trades:PositionInstance array) =
            
            let firstTradeIndex =
                trades
                |> Array.indexed
                |> Array.filter (fun (index,trade) -> trade.Closed.Value >= cutOff)
                |> Array.tryHead
                
            let span =
                match firstTradeIndex with
                | Some (index,_) -> Span<PositionInstance>(trades, index, trades.Length - index)
                | None -> Span<PositionInstance>(trades, 0, 0)
                
            span.ToArray()
        
        member _.ClosedPositions = closedPositions
        member _.RecentClosedPositions = Span<PositionInstance>(closedPositions, 0, recentLengthToTake);
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
        | "week" -> t.DateAsDate.AddDays(-(float)t.DateAsDate.DayOfWeek+1.0).ToString("MMMM dd, yyyy")
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
        
        member _.StockPL = plStockTransactions |> Seq.sumBy (fun (t:Transaction) -> t.Amount)
        member _.OptionPL = plOptionTransactions |> Seq.sumBy (fun (t:t:Transaction) -> t.Amount)