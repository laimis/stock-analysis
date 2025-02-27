namespace core.fs.Options

open System
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Options
    
type OptionContractView(
    underlyingTicker:core.Shared.Ticker,
    expiration:OptionExpiration,
    strikePrice:decimal,
    optionType:OptionType,
    longOrShort:LongOrShort,
    quantity:int,
    cost:decimal,
    instruction:OptionOrderInstruction option,
    chain:OptionChain option) =
    let chainDetail = chain |> Option.bind(_.FindMatchingOption(strikePrice, expiration, optionType))
    let underlyingPrice = chain |> Option.bind(_.UnderlyingPrice)
    let pctItm =
        match underlyingPrice with
        | Some(price) ->
            
            let itmPrice = 
                match optionType with
                | Call -> price - strikePrice
                | Put -> strikePrice - price
            itmPrice / price |> Some
        | None -> None
    let market = chainDetail |> Option.map(_.Mark)
        
    member this.Expiration = expiration
    member this.StrikePrice = strikePrice
    member this.OptionType = optionType
    member this.Quantity = quantity
    member this.IsShort = longOrShort = Short
    member this.Cost = cost
    member this.Market = market
    member this.Details = chainDetail
    member this.PctInTheMoney = pctItm
    member this.UnderlyingTicker = underlyingTicker
    member this.UnderlyingPrice = underlyingPrice
    member this.Instruction = instruction
    member this.BrokerageSymbol =
        match chainDetail with
        | Some detail -> detail.Symbol
        | None ->
            let ticker = underlyingTicker.Value.PadRight(6)
            let date = expiration.ToDateTimeOffset()
            let dateStr = date.ToString("yyMMdd")
            let optionTypeStr = match optionType with | Call -> "C" | Put -> "P"
            let strikeStr = (strikePrice * 1000m |> int).ToString("00000000")
            $"{ticker}{dateStr}{optionTypeStr}{strikeStr}"
            
    
type OptionPositionView(state:OptionPositionState, chain:OptionChain option) =
    
    let labels = state.Labels |> Seq.map id |> Seq.toArray
    let contracts =
        match state.Contracts.Count with
        | 0 ->
            state.PendingContracts.Keys
            |> Seq.map (fun k ->
                let (PendingContractQuantity(longOrShort, quantity)) = state.PendingContracts[k]
                OptionContractView(state.UnderlyingTicker, k.Expiration, k.Strike, k.OptionType, longOrShort, quantity, 0m, None, chain)
            )
            |> Seq.toList
        | _ ->
            state.Contracts.Keys
            |> Seq.map (fun k ->
                let (OpenedContractQuantityAndCost(longOrShort, quantity, cost)) = state.Contracts[k]
                let perContractCost =
                    match quantity with
                    | 0 -> 0m
                    | _ -> cost / decimal quantity |> abs
                OptionContractView(state.UnderlyingTicker, k.Expiration, k.Strike, k.OptionType, longOrShort, quantity, perContractCost, None, chain)
            )
            |> Seq.toList
        
    member this.PositionId = state.PositionId
    member this.UnderlyingTicker = state.UnderlyingTicker
    member this.UnderlyingPrice =
        match contracts with
        | [] -> None
        | _ -> contracts[0].UnderlyingPrice
    member this.Opened = state.Opened
    member this.DaysHeld = state.DaysHeld
    member this.DaysToExpiration =
        // days are countedfrom the time it's opened
        let referenceDate = 
            match state.Opened with
            | Some opened -> opened
            | None -> DateTimeOffset.Now

        contracts
        |> Seq.map (fun c -> c.Expiration.ToDateTimeOffset() - referenceDate)
        |> Seq.map (fun ts -> ts.TotalDays |> int)
        |> Seq.distinct
        |> Seq.sort
        
    member this.Closed = state.Closed
    member this.IsClosed = state.IsClosed
    member this.IsOpen = state.IsOpen
    member this.IsPending = state.IsPending
    member this.IsPendingClosed = state.IsPendingClosed
    member this.Cost =
        match state.Cost with
        | Some c -> c
        | None -> state.DesiredCost |> Option.defaultValue 0m
    member this.Market =
        contracts
        |> Seq.sumBy (fun c -> c.Details |> Option.map(fun o -> o.Mark * decimal c.Quantity) |> Option.defaultValue 0m)
    member this.Spread =
        match contracts with
        | [] -> 0m
        | _ ->
            // get min and max values of the contract strike prices
            let minStrike = contracts |> Seq.map _.StrikePrice |> Seq.min
            let maxStrike = contracts |> Seq.map _.StrikePrice |> Seq.max
            maxStrike - minStrike
    member this.Risked =
        // for spreads, the risk is the cost of the spread
        match contracts with
        | x when x.Length > 1 ->
            match this.Cost with
            | cost when cost < 0m -> this.Spread + this.Cost
            | _ -> this.Cost
        | x when x.Length = 1 -> 
            let contract = x[0]
            match contract.IsShort with
            | true -> 
                match contract.OptionType with
                | Call -> Double.PositiveInfinity |> decimal
                | Put -> contract.StrikePrice
            | false -> this.Cost
        | _ -> 0m
        
    member this.Profit =
        match this.IsClosed with
        | true -> state.Profit
        | false -> this.Market - this.Cost
    member this.GainPct = 
        match this.Risked with
        | 0m -> 0m
        | _ -> this.Profit / this.Risked
    member this.Transactions = state.Transactions
    member this.Notes = state.Notes
    member this.Labels = labels
    member this.Contracts = contracts
    
type OptionTradePerformanceMetrics = {
    // Basic statistics
    NumberOfTrades: int
    Wins: int
    Losses: int
    WinPct: decimal
    
    // Profit metrics
    TotalProfit: decimal
    AvgWinAmount: decimal
    MaxWinAmount: decimal
    AvgLossAmount: decimal
    MaxLossAmount: decimal
    
    // Risk-adjusted metrics
    SharpeRatio: decimal
    SortinoRatio: decimal
    ProfitFactor: decimal
    
    // Expectancy and R-multiples
    Expectancy: decimal
    AvgRMultiple: decimal
    
    // Drawdown metrics
    MaxDrawdown: decimal
    RecoveryFactor: decimal
    UlcerIndex: decimal
    
    // Risk management
    AvgRiskPerTrade: decimal
    AvgDaysHeld: decimal
    WinAvgDaysHeld: decimal
    LossAvgDaysHeld: decimal
    
    // Options-specific
    AvgIVPercentileEntry: decimal
    AvgIVPercentileExit: decimal
    AvgThetaPerDay: decimal
    
    // Return metrics
    AvgReturnPct: decimal
    WinAvgReturnPct: decimal
    LossAvgReturnPct: decimal
    ReturnPctRatio: decimal
    RiskAdjustedReturn: decimal
    
    // Consistency metrics
    ReturnStdDev: decimal
    DownsideDeviation: decimal
    
    // Strategy distribution
    StrategyDistribution: Map<string, int>
}

module OptionPerformance =
    let calculateDrawdowns (trades: OptionPositionView list) =
        if trades.IsEmpty then
            0m, 0m
        else
            let orderedTrades = trades |> List.sortBy (fun t -> t.Opened |> Option.defaultValue DateTimeOffset.MinValue)
            
            let mutable peakEquity = 0m
            let mutable currentEquity = 0m
            let mutable maxDrawdown = 0m
            let mutable drawdownSum = 0m
            let mutable drawdownDuration = 0
            let mutable maxDrawdownDuration = 0
            
            for trade in orderedTrades do
                currentEquity <- currentEquity + trade.Profit
                
                if currentEquity > peakEquity then
                    peakEquity <- currentEquity
                    drawdownDuration <- 0
                else
                    let drawdown = peakEquity - currentEquity
                    maxDrawdown <- max maxDrawdown drawdown
                    drawdownSum <- drawdownSum + drawdown * drawdown // For Ulcer Index
                    drawdownDuration <- drawdownDuration + 1
                    maxDrawdownDuration <- max maxDrawdownDuration drawdownDuration
                    
            let ulcerIndex = 
                if trades.Length > 0 then
                    sqrt (drawdownSum / decimal trades.Length |> float) |> decimal
                else
                    0m
                
            maxDrawdown, ulcerIndex
            
    let calculateRMultiple (trade: OptionPositionView) =
        if trade.Risked <= 0m then 1m  // Default if risk can't be determined
        else trade.Profit / trade.Risked
            
    let calculateReturns (trades: OptionPositionView list) =
        if trades.IsEmpty then
            0m, 0m, 0m
        else
            let returns = 
                trades 
                |> List.map (fun t -> t.GainPct)
            
            let mean = returns |> List.average
            
            let variance = 
                returns 
                |> List.map (fun r -> (r - mean) * (r - mean))
                |> List.average
                
            let stdDev = variance |> float |> sqrt |> decimal
            
            let downsideReturns = returns |> List.filter (fun r -> r < 0m)
            
            let downsideDeviation =
                if downsideReturns.IsEmpty then 0m
                else
                    let downsideVariance = 
                        downsideReturns 
                        |> List.map (fun r -> r * r)
                        |> List.average
                        |> float
                    sqrt downsideVariance |> decimal
                    
            mean, stdDev, downsideDeviation
    
    let calculatePerformanceMetrics (trades: OptionPositionView list) =
        if trades.IsEmpty then
            { 
                // Empty metrics with zeros
                NumberOfTrades = 0
                Wins = 0
                Losses = 0
                WinPct = 0m
                TotalProfit = 0m
                AvgWinAmount = 0m
                MaxWinAmount = 0m
                AvgLossAmount = 0m
                MaxLossAmount = 0m
                SharpeRatio = 0m
                SortinoRatio = 0m
                ProfitFactor = 0m
                Expectancy = 0m
                AvgRMultiple = 0m
                MaxDrawdown = 0m
                RecoveryFactor = 0m
                UlcerIndex = 0m
                AvgRiskPerTrade = 0m
                AvgDaysHeld = 0m
                WinAvgDaysHeld = 0m
                LossAvgDaysHeld = 0m
                AvgIVPercentileEntry = 0m
                AvgIVPercentileExit = 0m
                AvgThetaPerDay = 0m
                AvgReturnPct = 0m
                WinAvgReturnPct = 0m
                LossAvgReturnPct = 0m
                ReturnPctRatio = 0m
                RiskAdjustedReturn = 0m
                ReturnStdDev = 0m
                DownsideDeviation = 0m
                StrategyDistribution = Map.empty
            }
        else
            // Basic trade statistics
            let wins = trades |> List.filter (fun t -> t.Profit >= 0m)
            let losses = trades |> List.filter (fun t -> t.Profit < 0m)
            let winPct = decimal wins.Length / decimal trades.Length
            
            // Profit metrics
            let totalProfit = trades |> List.sumBy (fun t -> t.Profit)
            let avgWinAmount = if wins.IsEmpty then 0m else wins |> List.averageBy (fun t -> t.Profit)
            let maxWinAmount = if wins.IsEmpty then 0m else wins |> List.map (fun t -> t.Profit) |> List.max
            let avgLossAmount = if losses.IsEmpty then 0m else losses |> List.averageBy (fun t -> t.Profit) |> abs
            let maxLossAmount = if losses.IsEmpty then 0m else losses |> List.map (fun t -> t.Profit) |> List.min |> abs
            
            // Risk multiples
            let rMultiples = trades |> List.map calculateRMultiple
            let avgRMultiple = if rMultiples.IsEmpty then 0m else rMultiples |> List.average
            
            // Drawdown calculations
            let maxDrawdown, ulcerIndex = calculateDrawdowns trades
            
            // Recovery factor
            let recoveryFactor = 
                if maxDrawdown = 0m then 0m
                else totalProfit / maxDrawdown
                
            // Risk per trade
            let avgRiskPerTrade = trades |> List.map (fun t -> t.Risked) |> List.average
            
            // Days held
            let avgDaysHeld = 
                trades 
                |> List.map (fun t -> t.DaysHeld |> Option.defaultValue 0) 
                |> List.averageBy decimal
                
            let winAvgDaysHeld = 
                if wins.IsEmpty then 0m
                else wins |> List.map (fun t -> t.DaysHeld |> Option.defaultValue 0) |> List.averageBy decimal
                
            let lossAvgDaysHeld = 
                if losses.IsEmpty then 0m
                else losses |> List.map (fun t -> t.DaysHeld |> Option.defaultValue 0) |> List.averageBy decimal
                
            // Return calculations
            let avgReturnPct, returnStdDev, downsideDeviation = calculateReturns trades
            
            let winAvgReturnPct = 
                if wins.IsEmpty then 0m
                else 
                    wins 
                    |> List.map (fun t -> match t.Cost with | 0m -> 0m | cost -> t.Profit / abs cost) 
                    |> List.average
                    
            let lossAvgReturnPct = 
                if losses.IsEmpty then 0m
                else 
                    losses 
                    |> List.map (fun t -> match t.Cost with | 0m -> 0m | cost -> t.Profit / abs cost) 
                    |> List.average
            
            // Profit factor
            let grossProfit = wins |> List.sumBy (fun t -> t.Profit)
            let grossLoss = losses |> List.sumBy (fun t -> abs t.Profit)
            let profitFactor = if grossLoss = 0m then 0m else grossProfit / grossLoss
            
            // Expectancy
            let expectancy = (winPct * avgWinAmount) - ((1m - winPct) * avgLossAmount)
            
            // Return % ratio
            let returnPctRatio = 
                match winAvgReturnPct, lossAvgReturnPct with
                | w, l when w > 0m && l < 0m -> w / abs l
                | _ -> 0m
                
            // Sharpe and Sortino ratios (assuming risk-free rate of 0 for simplicity)
            let sharpeRatio = 
                if returnStdDev = 0m then 0m
                else avgReturnPct / returnStdDev
                
            let sortinoRatio = 
                if downsideDeviation = 0m then 0m
                else avgReturnPct / downsideDeviation
                
            // Risk-adjusted return
            let riskAdjustedReturn = 
                if avgRiskPerTrade = 0m then 0m
                else avgReturnPct / avgRiskPerTrade
                
            // Strategy distribution
            let strategyDistribution =
                trades
                |> List.choose (fun t -> 
                    t.Labels 
                    |> Array.tryFind (fun label -> label.Key = "strategy") 
                    |> Option.map (fun label -> label.Value))
                |> List.groupBy id
                |> List.map (fun (strategy, occurrences) -> strategy, occurrences.Length)
                |> Map.ofList
                
            // IV percentiles and theta - placeholders as we might need to add these data points to OptionPositionView
            let avgIVPercentileEntry = 0m // Placeholder
            let avgIVPercentileExit = 0m  // Placeholder
            let avgThetaPerDay = 0m       // Placeholder
            
            {
                NumberOfTrades = trades.Length
                Wins = wins.Length
                Losses = losses.Length
                WinPct = winPct
                TotalProfit = totalProfit
                AvgWinAmount = avgWinAmount
                MaxWinAmount = maxWinAmount
                AvgLossAmount = avgLossAmount
                MaxLossAmount = maxLossAmount
                SharpeRatio = sharpeRatio
                SortinoRatio = sortinoRatio
                ProfitFactor = profitFactor
                Expectancy = expectancy
                AvgRMultiple = avgRMultiple
                MaxDrawdown = maxDrawdown
                RecoveryFactor = recoveryFactor
                UlcerIndex = ulcerIndex
                AvgRiskPerTrade = avgRiskPerTrade
                AvgDaysHeld = avgDaysHeld
                WinAvgDaysHeld = winAvgDaysHeld
                LossAvgDaysHeld = lossAvgDaysHeld
                AvgIVPercentileEntry = avgIVPercentileEntry
                AvgIVPercentileExit = avgIVPercentileExit
                AvgThetaPerDay = avgThetaPerDay
                AvgReturnPct = avgReturnPct
                WinAvgReturnPct = winAvgReturnPct
                LossAvgReturnPct = lossAvgReturnPct
                ReturnPctRatio = returnPctRatio
                RiskAdjustedReturn = riskAdjustedReturn
                ReturnStdDev = returnStdDev
                DownsideDeviation = downsideDeviation
                StrategyDistribution = strategyDistribution
            }

type OptionPerformanceView = {
    Total: OptionTradePerformanceMetrics
    ByTimeframes: Map<string, OptionTradePerformanceMetrics>
}

type OptionPositionStats(summaries:seq<OptionPositionView>) =
    
    let optionTrades = summaries |> Seq.toList
    let wins = optionTrades |> List.filter (fun s -> s.Profit >= 0m)
    let losses = optionTrades |> List.filter (fun s -> s.Profit < 0m)
    
    member this.Count = optionTrades |> List.length
    member this.Wins = wins |> List.length
    member this.AvgWinAmount =
        match wins with
        | [] -> None
        | _ -> wins |> List.averageBy (fun s -> s.Profit) |> Some
        
    member this.MaxWinAmount =
        match wins with
        | [] -> None
        | _ -> wins |> List.map (fun s -> s.Profit) |> List.max |> Some
        
    member this.Losses = losses |> List.length
    member this.AverageLossAmount =
        match losses with
        | [] -> None
        | _ -> losses |> List.averageBy (fun s -> s.Profit) |> abs |> Some
        
    member this.MaxLossAmount =
        match losses with
        | [] -> None
        | _ -> losses |> List.map (fun s -> s.Profit) |> List.min |> abs |> Some
    
    member this.EV =
        match (this.AvgWinAmount, this.AverageLossAmount) with
        | Some avgWinAmount, Some avgLossAmount ->
            let winPart = (avgWinAmount * decimal this.Wins / decimal this.Count)
            let lossPart = (avgLossAmount * decimal this.Losses / decimal this.Count)
            winPart - lossPart |> Some
        | _ -> None
            
    member this.AverageProfitPerDay =
        match optionTrades with
        | [] -> 0m
        | _ ->
            optionTrades
            |> List.map (fun s -> s.Profit / decimal (s.DaysHeld |> Option.defaultValue 0)) |> List.average
        
    member this.AverageDays =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal (s.DaysToExpiration |> Seq.head)) |> List.average
    member this.AverageDaysHeld =
        match optionTrades with
        | [] -> 0m
        | _ -> optionTrades |> List.map (fun s -> decimal (s.DaysHeld |> Option.defaultValue 0)) |> List.average
    member this.AverageDaysHeldPercentage =
        match optionTrades with
        | [] -> 0m
        | _ ->
            optionTrades
            |> List.map (fun s ->
                let daysHeld = s.DaysHeld |> Option.defaultValue 0
                let daysToExpiration = s.DaysToExpiration |> Seq.head
                match daysToExpiration with
                | 0 -> 0m
                | _ -> decimal daysHeld / decimal daysToExpiration
            )
            |> List.average
    
type OptionOrderView(order:OptionOrder, chain:OptionChain option) =
    member this.OrderId = order.OrderId
    member this.Price = order.Price
    member this.Quantity = order.Quantity
    member this.Status = order.Status
    member this.Type = order.Type
    member this.ExecutionTime = order.ExecutionTime
    member this.EnteredTime = order.EnteredTime
    member this.ExpirationTime = order.ExpirationTime
    member this.CanBeCancelled = order.CanBeCancelled
    member this.CanBeRecorded = order.CanBeRecorded
    member this.IsActive = order.IsActive
    member this.Contracts =
        order.Contracts |> Seq.map (fun l ->
            let longOrShort = if l.Quantity > 0 then Long else Short
            OptionContractView(l.UnderlyingTicker, l.Expiration, l.StrikePrice, l.OptionType, longOrShort, l.Quantity, l.Price |> Option.defaultValue 0m, l.Instruction |> Some, chain)
        )
    
type OptionDashboardView(pending:seq<OptionPositionView>,``open``:seq<OptionPositionView>,closed:seq<OptionPositionView>, brokeragePositions:seq<BrokerageOptionPosition>, orders:seq<OptionOrderView>) =
    
    let closedList = closed |> Seq.toList
    
    let overallPerformance = OptionPerformance.calculatePerformanceMetrics closedList

    // Generate timeframe-based performance metrics
    let now = DateTimeOffset.UtcNow
    
    let last20 = 
        closedList 
        |> List.sortByDescending (fun p -> p.Closed |> Option.defaultValue DateTimeOffset.MaxValue)
        |> List.truncate 20
        
    let last50 = 
        closedList 
        |> List.sortByDescending (fun p -> p.Closed |> Option.defaultValue DateTimeOffset.MaxValue)
        |> List.truncate 50
        
    let last100 = 
        closedList 
        |> List.sortByDescending (fun p -> p.Closed |> Option.defaultValue DateTimeOffset.MaxValue)
        |> List.truncate 100
        
    let last2Months = 
        closedList 
        |> List.filter (fun p -> 
            match p.Closed with
            | Some closed -> closed >= now.AddMonths(-2)
            | None -> false)
            
    let startOfYear = DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero)
    let ytd =
        closedList
        |> List.filter (fun p ->
            match p.Closed with
            | Some closed -> closed >= startOfYear
            | None -> false)
            
    let oneYear =
        closedList
        |> List.filter (fun p ->
            match p.Closed with
            | Some closed -> closed >= now.AddYears(-1)
            | None -> false)
            
    let timeframePerformance = 
        [
            "Last 20", OptionPerformance.calculatePerformanceMetrics last20
            "Last 50", OptionPerformance.calculatePerformanceMetrics last50
            "Last 100", OptionPerformance.calculatePerformanceMetrics last100
            "Last 2 Months", OptionPerformance.calculatePerformanceMetrics last2Months
            "YTD", OptionPerformance.calculatePerformanceMetrics ytd
            "1 Year", OptionPerformance.calculatePerformanceMetrics oneYear
            "All Time", overallPerformance
        ]
        |> Map.ofList

    member this.Closed = closed
    member this.Open = ``open``
    member this.Pending = pending
    member this.Orders = orders
    member this.BrokeragePositions = brokeragePositions
    member this.OverallStats = OptionPositionStats(closed)
    member this.BuyStats = OptionPositionStats([])
    member this.SellStats = OptionPositionStats([])
    member this.Performance = {
        Total = overallPerformance
        ByTimeframes = timeframePerformance
    }

type OptionChainView(chain:OptionChain) =
    
    member this.StockPrice = chain.UnderlyingPrice
    member this.Options = chain.Options
    member this.Expirations = chain.Options |> Seq.map _.Expiration |> Seq.distinct |> Seq.toArray
    member this.Volatility = chain.Volatility
    member this.NumberOfContracts = chain.NumberOfContracts
