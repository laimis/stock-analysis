namespace core.fs.Services.Trading

open System
open System.Collections.Generic
open core.Account
open core.Shared
open core.Stocks
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Logging
open core.fs.Adapters.Stocks
open core.fs.Stocks

module TradingStrategyConstants =
    // TODO: this needs to come from the environment or user settings
    let AvgPercentGain = 0.07m
    let DefaultStopPriceMultiplier = 0.95m
    let MaxNumberOfDaysToSimulate = 365
    let ActualTradesName = "Actual trades ⭐";
    
type SimulationContext =
    {
        Position:StockPositionWithCalculations
        MaxGainFirst10Bars:decimal
        MaxGain:decimal
        MaxDrawdownFirst10Bars:decimal
        MaxDrawdown:decimal
        BarCount:int
        LastBar:PriceBar
    }

type TradingStrategyResult =
    {
        MaxDrawdownPct:decimal
        MaxDrawdownFirst10Bars:decimal
        MaxGainPct:decimal
        MaxGainFirst10Bars:decimal
        Position:StockPositionWithCalculations
        StrategyName:string
        ForcedClosed:bool
    }

type TradingStrategyResults() =
    
    let results = List<TradingStrategyResult>()
    
    member val Results = results.AsReadOnly() with get, set
    
    member this.Add(result:TradingStrategyResult) =
        results.Add(result)
        
    member this.Insert(index:int, result:TradingStrategyResult) =
        results.Insert(index, result)
        
    member val FailedReason:string option = None with get, set
    
    member this.MarkAsFailed(reason:string) =
        this.FailedReason <- Some reason

type ITradingStrategy =
    abstract Run : bars:PriceBars -> closeIfOpen:bool -> position:StockPositionState -> TradingStrategyResult
        
module ProfitPoints =
    
    type ProfitPointContainer(name:string, prices:decimal seq) =
        member this.Name = name
        member this.Prices = prices
        
    let getProfitPointWithStopPrice (level:int) (position:StockPositionWithCalculations)  =
        let riskPerShare = position.CompletedPositionCostPerShare - position.FirstStop().Value
        position.CompletedPositionCostPerShare + riskPerShare * decimal(level)
     
    let getProfitPointWithPercentGain (level:int) (percentGain:decimal) (position:StockPositionWithCalculations) =
        let singleLevel = position.CompletedPositionCostPerShare * percentGain
        match position.IsShort with
        | true -> position.CompletedPositionCostPerShare - singleLevel * decimal(level)
        | false -> position.CompletedPositionCostPerShare + singleLevel * decimal(level)
    
    let getProfitPointsWithStopPrice levels position =
        let profitPoints =
            [1..levels]
            |> List.map (fun l -> position |> getProfitPointWithStopPrice l)
            
        profitPoints
    
    let getProfitPoints func levels =
        [1..levels]
        |> List.map (fun level -> func level)
        
module TradingPerformance =
    let calculateMaxDrawdown (closedPositions:StockPositionWithCalculations seq) =
        
        closedPositions
        |> Seq.fold (fun (maxDrawdown, currentDrawdown) position ->
            let newDrawdown = 
                match currentDrawdown + position.Profit with
                | x when x < 0m -> x
                | _ -> 0m
            let newMaxDrawdown = Math.Min(maxDrawdown, newDrawdown)
            (newMaxDrawdown, newDrawdown)
        ) (0m, 0m)
        |> fst
        
    let calculateMaxCashNeeded (closedPositions:StockPositionWithCalculations seq) =
        match closedPositions |> Seq.isEmpty with
        | true -> 0m
        | false ->
        // I need to calculate the max amount of cash needed to execute this
        // strategy. It cannot be a simple sum of all the costs and needs to be
        // a day by day calculations starting from the earliest open to the latest close
        let earliestOpen = closedPositions |> Seq.minBy _.Opened
        let latestClose = closedPositions |> Seq.maxBy _.Closed.Value
        
        let mapByDateOpened = 
            closedPositions
            |> Seq.groupBy _.Opened.Date
            |> Seq.map (fun (date, positions) -> date, positions |> Seq.map (fun x -> x.Cost))
            |> Map.ofSeq
            
        let mapByDateClosed = 
            closedPositions
            |> Seq.groupBy _.Closed.Value.Date
            |> Seq.map (fun (date, positions) -> date, positions |> Seq.map (fun x -> x.Cost))
            |> Map.ofSeq
            
        let dates = Array.init ((int (latestClose.Closed.Value.Date - earliestOpen.Opened.Date).TotalDays) + 1) (fun i -> earliestOpen.Opened.Date.AddDays(float i))
        
        let cashNeeded, maxCashNeed = 
            dates
            |> Array.fold (fun (currentCashNeeded, maxValue) date ->
                
                let cashNeededToday =
                    match mapByDateOpened.TryFind date with
                    | Some costs -> costs |> Seq.sum
                    | None -> 0m
                    
                let cashNotNeededToday =
                    match mapByDateClosed.TryFind date with
                    | Some costs -> costs |> Seq.sum
                    | None -> 0m
                    
                let newCashNeed = currentCashNeeded + cashNeededToday - cashNotNeededToday
                let newMaxValue = Math.Max(maxValue, newCashNeed)
                (newCashNeed, newMaxValue)
            ) (0m, 0m)
            
        if Math.Round(cashNeeded, 2) <> 0m then // we need to round because of floating point errors
            failwith $"Cash needed should be zeroed out, but is {cashNeeded}"
            
        maxCashNeed

type TradingPerformance =
    {
        Name:string
        NumberOfTrades:int
        Wins:int
        WinAmount:decimal
        MaxWinAmount:decimal
        TotalDaysHeldWins:decimal
        WinReturnPctTotal:decimal
        WinMaxReturnPct:decimal
        WinRRTotal:decimal
        Losses:int
        LossAmount:decimal
        MaxLossAmount:decimal
        LossReturnPctTotal:decimal
        LossMaxReturnPct:decimal
        LossRRTotal:decimal
        TotalDaysHeldLosses:decimal
        Profit:decimal
        GainPctSum:decimal
        GainPctStdDev:decimal
        rrSum:decimal
        CostSum:decimal
        TotalDaysHeld:decimal
        EarliestDate:DateTimeOffset
        LatestDate:DateTimeOffset
        MaxCashNeeded:decimal
        MaxDrawdown:decimal
        GradeDistribution:LabelWithFrequency[]
    }
        with
            
            member this.AvgWinAmount =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.WinAmount / decimal(this.Wins)
                
            member this.AvgLossAmount =
                match this.Losses with
                | 0 -> 0m
                | _ -> this.LossAmount / decimal(this.Losses)
            
            member this.WinAvgReturnPct =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.WinReturnPctTotal / decimal(this.Wins)
                
            member this.LossAvgReturnPct =
                match this.Losses with
                | 0 -> 0m
                | _ -> this.LossReturnPctTotal / decimal(this.Losses)
        
            member this.WinAvgDaysHeld =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.TotalDaysHeldWins / decimal(this.Wins)
                
            member this.LossAvgDaysHeld =
                match this.Losses with
                | 0 -> 0m
                | _ -> this.TotalDaysHeldLosses / decimal(this.Losses)
              
            member this.ReturnPctRatio =
                match this.LossAvgReturnPct with
                | 0m -> 0m
                | _ -> this.WinAvgReturnPct / this.LossAvgReturnPct |> Math.Abs
                
            member this.ProfitRatio =
                match this.AvgLossAmount with
                | 0m -> 0m
                | _ -> this.AvgWinAmount / this.AvgLossAmount |> Math.Abs
                
            member this.AverageDaysHeld =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.TotalDaysHeld / decimal(this.NumberOfTrades)
                
            member this.WinPct = 
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> decimal(this.Wins) / decimal(this.NumberOfTrades)
                
            member this.EV =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.WinPct * this.AvgWinAmount - (1m - this.WinPct) * (this.AvgLossAmount |> abs)
                
            member this.WinAvgRR =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.WinRRTotal / decimal this.Wins
                
            member this.LossAvgRR =
                match this.Losses with
                | 0 -> 0m
                | _ -> this.LossRRTotal / decimal this.Losses
                
            member this.AverageRR =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.rrSum / decimal this.NumberOfTrades
                
            member this.rrRatio =
                match (this.WinAvgRR, this.LossAvgRR) with
                | 0m, _ -> this.LossAvgRR
                | _, 0m -> this.WinAvgRR
                | _ -> this.WinAvgRR / this.LossAvgRR |> Math.Abs
                
            member this.AvgReturnPct =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.GainPctSum / decimal this.NumberOfTrades
                
            member this.AvgCost =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.CostSum / decimal this.NumberOfTrades
                
            member this.SharpeRatio =
                match this.GainPctStdDev with
                | 0m -> 0m
                | _ -> this.AvgReturnPct / this.GainPctStdDev
                
            member this.RiskAdjustedReturn =
                match this.MaxCashNeeded with
                | 0m -> 0m
                | _ -> this.Profit / this.MaxCashNeeded
                
            static member Create name (closedPositions:seq<StockPositionWithCalculations>) =
                
                let maxCashNeeded = TradingPerformance.calculateMaxCashNeeded closedPositions
                let maxDrawdown = TradingPerformance.calculateMaxDrawdown closedPositions
                    
                let gainStats = MathNet.Numerics.Statistics.RunningStatistics()
                
                closedPositions
                |> Seq.fold (fun perf position ->
                    
                    gainStats.Push(position.GainPct |> float)
                    
                    let gainPctStDev =
                        match gainStats.StandardDeviation with
                        | x when Double.IsNaN(x) -> 0m
                        | x -> x |> decimal
                    
                    {
                        Name = name
                        NumberOfTrades = perf.NumberOfTrades + 1
                        TotalDaysHeld = perf.TotalDaysHeld + decimal(position.DaysHeld)
                        Profit = perf.Profit + position.Profit
                        GainPctSum = perf.GainPctSum + position.GainPct
                        GainPctStdDev = gainPctStDev 
                        rrSum = perf.rrSum + position.RR
                        CostSum = perf.CostSum + position.CompletedPositionCostPerShare * decimal(position.CompletedPositionShares)
                        EarliestDate = if position.Opened < perf.EarliestDate then position.Opened else perf.EarliestDate
                        LatestDate = if position.Closed.IsSome && position.Closed.Value > perf.LatestDate then position.Closed.Value else perf.LatestDate
                        MaxCashNeeded = maxCashNeeded
                        MaxDrawdown = maxDrawdown
                        GradeDistribution = 
                            match position.Grade with
                            | Some grade ->
                                let gradeDistribution =
                                    match perf.GradeDistribution |> Array.tryFind (fun g -> g.label = grade.Value) with
                                    | Some _ -> perf.GradeDistribution |> Array.map (fun g -> if g.label = grade.Value then { g with frequency = g.frequency + 1 } else g)
                                    | None -> perf.GradeDistribution |> Array.append [| { label = grade.Value; frequency = 1 } |]
                                gradeDistribution |> Array.sortBy _.label
                            | None -> perf.GradeDistribution
                            
                        Wins = 
                            match position.Profit >= 0m with
                            | true -> perf.Wins + 1
                            | false -> perf.Wins
                        
                        WinAmount =
                            match position.Profit >= 0m with
                            | true -> perf.WinAmount + position.Profit
                            | false -> perf.WinAmount
                            
                        MaxWinAmount =
                            
                            match position.Profit >= 0m with
                            | true -> Math.Max(perf.MaxWinAmount, position.Profit)
                            | false -> perf.MaxWinAmount
                            
                        TotalDaysHeldWins =
                            match position.Profit >= 0m with
                            | true -> perf.TotalDaysHeldWins + decimal(position.DaysHeld)
                            | false -> perf.TotalDaysHeldWins
                        
                        TotalDaysHeldLosses =
                            match position.Profit < 0m with
                            | true -> perf.TotalDaysHeldLosses + decimal(position.DaysHeld)
                            | false -> perf.TotalDaysHeldLosses
                            
                        WinReturnPctTotal =
                            match position.Profit >= 0m with
                            | true -> perf.WinReturnPctTotal + position.GainPct
                            | false -> perf.WinReturnPctTotal
                            
                        Losses = 
                            match position.Profit < 0m with
                            | true -> perf.Losses + 1
                            | false -> perf.Losses
                        
                        LossAmount =
                            match position.Profit < 0m with
                            | true -> perf.LossAmount + position.Profit
                            | false -> perf.LossAmount
                        
                        MaxLossAmount =
                            match position.Profit < 0m with
                            | true -> Math.Min(perf.MaxLossAmount, position.Profit)
                            | false -> perf.MaxLossAmount
                            
                        WinMaxReturnPct =
                            match position.Profit >= 0m with
                            | true -> Math.Max(perf.WinMaxReturnPct, position.GainPct)
                            | false -> perf.WinMaxReturnPct
                            
                        LossMaxReturnPct =
                            match position.Profit < 0m with
                            | true -> Math.Min(perf.LossMaxReturnPct, position.GainPct)
                            | false -> perf.LossMaxReturnPct
                            
                        LossReturnPctTotal =
                            match position.Profit < 0m with
                            | true -> perf.LossReturnPctTotal + position.GainPct
                            | false -> perf.LossReturnPctTotal
                            
                        WinRRTotal =
                            match position.Profit >= 0m with
                            | true -> perf.WinRRTotal + position.RR
                            | false -> perf.WinRRTotal
                            
                        LossRRTotal =
                            match position.Profit < 0m with
                            | true -> perf.LossRRTotal + position.RR
                            | false -> perf.LossRRTotal
                    }
                ) { NumberOfTrades = 0
                    Wins = 0
                    WinAmount = 0m
                    MaxWinAmount = 0m
                    Profit = 0m
                    WinMaxReturnPct = 0m
                    Losses = 0
                    LossAmount = 0m
                    MaxLossAmount = 0m
                    LossMaxReturnPct = 0m
                    TotalDaysHeld = 0m
                    rrSum = 0m
                    EarliestDate = DateTimeOffset.MaxValue
                    LatestDate = DateTimeOffset.MinValue
                    GradeDistribution = [||]
                    GainPctSum = 0m
                    GainPctStdDev = 0m
                    CostSum = 0m 
                    TotalDaysHeldWins = 0m
                    TotalDaysHeldLosses = 0m 
                    WinReturnPctTotal = 0m
                    LossReturnPctTotal = 0m
                    WinRRTotal = 0m
                    LossRRTotal = 0m
                    Name = name
                    MaxCashNeeded = 0m
                    MaxDrawdown = 0m
                }

type TradingStrategyPerformance =
    {
        strategyName : string
        performance: TradingPerformance
        results: TradingStrategyResult[]
    }
    
    with
        member this.NumberOfOpenPositions =
            this.results
            |> Array.filter _.Position.IsOpen
            |> Array.length
            
[<AbstractClass>]
type TradingStrategy(name:string) =
    
    let mutable _numberOfSharesAtStart = 0m
    
    static member GetPercentDiffFromReferencePrice (bar:PriceBar) (context:SimulationContext) =
        let referencePrice = context.Position.PositionOpenPrice
        match context.Position.IsShort with
        | true -> (referencePrice - bar.Close) / referencePrice
        | false -> bar.percentDifferenceFromClose(referencePrice)
        
    static member CalculateMAEPercentage (context:SimulationContext) (bar:PriceBar) =
        let difference = context |> TradingStrategy.GetPercentDiffFromReferencePrice bar
        Math.Min(context.MaxDrawdown, difference)    
    
    static member CalculateMFEPercentage (context:SimulationContext) (bar:PriceBar) =
        let difference = context |> TradingStrategy.GetPercentDiffFromReferencePrice bar
        Math.Max(context.MaxGain, difference)
    
    static member ClosePosition price date reason (position:StockPositionState) =
        match position.IsOpen with
        | true ->
            position
            |> StockPosition.addNotes (Some reason) date
            |> StockPosition.close price date
        | false -> position
        
    static member ForceCloseIfNecessary closeIfOpen (context:SimulationContext) =
        match closeIfOpen && context.Position.IsOpen with
        | true ->
            (context.Position.GetPositionState()
            |> TradingStrategy.ClosePosition context.LastBar.Close context.LastBar.Date "Force Close"
            |> StockPositionWithCalculations, true)
        | false -> (context.Position, false)
    
    member this.Name = name
    member this.NumberOfSharesAtStart = _numberOfSharesAtStart
    
    abstract member ApplyPriceBarToPositionInternal : SimulationContext -> PriceBar -> StockPositionState
    
    member private this.ApplyPriceBarToPosition (context:SimulationContext) (bar:PriceBar) =
        
        match context.Position.Closed with
        | Some _ -> context
        | None ->
            let latestPositionState = this.ApplyPriceBarToPositionInternal context bar
            
            let maxDrawdownPct = TradingStrategy.CalculateMAEPercentage context bar
            let maxDrawdownFirst10Bars = if context.BarCount < 10 then maxDrawdownPct else context.MaxDrawdownFirst10Bars
            let maxGainPct = TradingStrategy.CalculateMFEPercentage context bar
            let maxGainFirst10Bars = if context.BarCount < 10 then maxGainPct else context.MaxGainFirst10Bars
            
            {
                Position = latestPositionState |> StockPositionWithCalculations
                MaxDrawdown = TradingStrategy.CalculateMAEPercentage context bar
                MaxDrawdownFirst10Bars = maxDrawdownFirst10Bars 
                MaxGain = TradingStrategy.CalculateMFEPercentage context bar
                MaxGainFirst10Bars = maxGainFirst10Bars 
                LastBar = bar
                BarCount = context.BarCount + 1 
            }
           
    interface  ITradingStrategy with
    
        member this.Run (bars:PriceBars) closeIfOpen (position:StockPositionState) =
            
            let context = 
                {
                    Position = position |> StockPositionWithCalculations
                    MaxDrawdown = Decimal.MaxValue
                    MaxDrawdownFirst10Bars = Decimal.MaxValue
                    MaxGain = Decimal.MinValue
                    MaxGainFirst10Bars = Decimal.MinValue
                    BarCount = 0
                    LastBar = bars.Bars[0] 
                }
                
            _numberOfSharesAtStart <- position.NumberOfShares |> abs
                
            let finalContext =
                bars.Bars
                |> Seq.fold this.ApplyPriceBarToPosition context
                
            let finalPosition, forcedClosed = TradingStrategy.ForceCloseIfNecessary closeIfOpen finalContext
            
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxDrawdownFirst10Bars = finalContext.MaxDrawdownFirst10Bars
                MaxGainPct = finalContext.MaxGain
                MaxGainFirst10Bars = finalContext.MaxGainFirst10Bars
                Position = finalPosition
                StrategyName = this.Name
                ForcedClosed = forcedClosed 
            }

type TradingStrategyCloseOnCondition(name:string,exitCondition) =
    
    inherit TradingStrategy(name)
    
    override this.ApplyPriceBarToPositionInternal (context:SimulationContext) (bar:PriceBar) =
        let doExit, stopPrice = exitCondition context bar
        
        let positionAfterStopAdjustment =
            match stopPrice with
            | Some _ ->
                context.Position.GetPositionState() |> StockPosition.setStop stopPrice bar.Date
            | None -> context.Position.GetPositionState()
            
        if doExit then
            positionAfterStopAdjustment |> TradingStrategy.ClosePosition bar.Close bar.Date "Exit triggered"
        else
            positionAfterStopAdjustment

type TradingStrategyActualTrade() =
    
    interface ITradingStrategy with
    
        member this.Run (bars:PriceBars) (closeIfOpen:bool) (position:StockPositionState) =
            
            let calcs = position |> StockPositionWithCalculations
            let finalContext =
                bars.Bars
                |> Seq.fold (fun (context:SimulationContext) bar ->
                    if position.IsClosed && bar.Date.Date > position.Closed.Value.Date then
                        context
                    else
                        let maxDrawdownPct = TradingStrategy.CalculateMAEPercentage context bar
                        let maxDrawdownFirst10Bars = if context.BarCount < 10 then maxDrawdownPct else context.MaxDrawdownFirst10Bars
                        let maxGainPct = TradingStrategy.CalculateMFEPercentage context bar
                        let maxGainFirst10Bars = if context.BarCount < 10 then maxGainPct else context.MaxGainFirst10Bars
                        
                        { Position = calcs; MaxDrawdown = maxDrawdownPct; MaxGain = maxGainPct; LastBar = bar; BarCount = context.BarCount + 1; MaxDrawdownFirst10Bars = maxDrawdownFirst10Bars; MaxGainFirst10Bars = maxGainFirst10Bars }
                ) {Position = calcs; MaxDrawdown = Decimal.MaxValue; MaxGain = Decimal.MinValue; LastBar = bars.Bars[0]; BarCount = 0; MaxDrawdownFirst10Bars = Decimal.MaxValue; MaxGainFirst10Bars = Decimal.MinValue}
                
            let finalPosition, forcedClosed = TradingStrategy.ForceCloseIfNecessary closeIfOpen finalContext
                
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxDrawdownFirst10Bars = finalContext.MaxDrawdownFirst10Bars
                MaxGainPct = finalContext.MaxGain
                MaxGainFirst10Bars = finalContext.MaxGainFirst10Bars
                Position = finalPosition
                StrategyName = TradingStrategyConstants.ActualTradesName
                ForcedClosed = forcedClosed
            }

type TradingStrategyWithProfitPoints(name:string,numberOfProfitPoints,profitPointFunc,stopPriceFunc) =
    
    inherit TradingStrategy(name)
    
    let mutable _level = 1
    
    member this.ExecuteProfitTake sellPrice date (position:StockPositionWithCalculations) =
        
        let executeProfitTake portion price date (position:StockPositionWithCalculations) =
            match position.IsShort with
            | true -> position.GetPositionState() |> StockPosition.buy portion price date
            | false -> position.GetPositionState() |> StockPosition.sell portion price date
            
        // figure out how much to sell based on the number of profit points
        // and how many shares we have left
        let portion =
            if _level = numberOfProfitPoints then
                position.NumberOfShares
            else
                match int (this.NumberOfSharesAtStart / decimal numberOfProfitPoints) |> decimal with
                | 0m -> 1m
                | x when x > (abs position.NumberOfShares) -> position.NumberOfShares    
                | x -> x
            |> abs // it can be negative if we're shorting
        
        let adjustStopIfNecessary (position:StockPositionState) =
            match position.IsOpen with
            | true ->
                let stopPrice:decimal = position |> StockPositionWithCalculations |> stopPriceFunc _level
                position |> StockPosition.setStop (Some stopPrice) date
            | _ -> position
        
            
        let afterSell =
            position
            |> executeProfitTake portion sellPrice date
            |> adjustStopIfNecessary
        
        _level <- _level + 1
        
        afterSell
            
    override this.ApplyPriceBarToPositionInternal context bar =
        
        let profitPrice = context.Position |> profitPointFunc _level
                
        let stopReached price (position:StockPositionState) =
            match position.StopPrice with
            | Some stopPrice ->
                match position.StockPositionType with
                | Short -> price >= stopPrice
                | Long -> price <= stopPrice
            | _ -> false
            
        let profitTakeReached (bar:PriceBar) (position:StockPositionWithCalculations) =
            match position.IsShort with
            | true -> bar.Low < profitPrice
            | false -> bar.High > profitPrice
        
        let executeProfitTakeIfNecessary (position:StockPositionWithCalculations) =
            match profitTakeReached bar position with
            | true -> this.ExecuteProfitTake profitPrice bar.Date position
            | false -> position.GetPositionState()
        
        let closeIfNecessary (position:StockPositionState) =
            match stopReached bar.Close position with
            | true -> TradingStrategy.ClosePosition bar.Close bar.Date "Stop loss" position
            | _ -> position
        
        context.Position
        |> executeProfitTakeIfNecessary
        |> closeIfNecessary
            
            
module TradingStrategyFactory =
    
    let advancingStop (level:int) (position:StockPositionWithCalculations) rrFunc =
        match level with
        | 1 -> position.AverageCostPerShare
        | _ -> rrFunc (level - 1) position
    
    let delayedAdvancingStop (level:int) (position:StockPositionWithCalculations) (rrLevelFunc:int -> decimal) =
        match level with
        | 1 -> position.StopPrice.Value
        | 2 -> position.AverageCostPerShare
        | _ -> rrLevelFunc (level - 2)
    
    let createActualTrade() : ITradingStrategy = TradingStrategyActualTrade()
    let createProfitPointsTrade numberOfProfitPoints : ITradingStrategy =
        let stopFunc = fun (level:int) position -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
        TradingStrategyWithProfitPoints($"Profit points: {numberOfProfitPoints}", numberOfProfitPoints, ProfitPoints.getProfitPointWithStopPrice, stopFunc)
        
    let createBuyAndHold : ITradingStrategy =
        let exitCondition = fun (_:SimulationContext) (_:PriceBar) ->
            false, None
        TradingStrategyCloseOnCondition("Buy and hold", exitCondition)
    
    let createProfitPointsBasedOnPctGainTrade percentGain numberOfProfitPoints : ITradingStrategy =
        if percentGain <= 0m then
            failwith "Percent gain must be greater than 0"
        if percentGain >= 1m then
            failwith "Percent gain must be less than 1, e.g. 0.07 for 7%"
            
        let profitPointFunc = fun (level:int) -> ProfitPoints.getProfitPointWithPercentGain level percentGain
        let stopFunc = fun (level:int) (position:StockPositionWithCalculations) -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
        TradingStrategyWithProfitPoints($"{numberOfProfitPoints} profit points at {percentGain}%% intervals", numberOfProfitPoints, profitPointFunc, stopFunc)
        
    let createCloseAfterFixedNumberOfDays numberOfDays : ITradingStrategy =
        let exitCondition = fun (context:SimulationContext) (bar:PriceBar) ->
            let doExit = bar.Date - context.Position.Opened > TimeSpan.FromDays(float numberOfDays)
            doExit, None
        TradingStrategyCloseOnCondition($"Close after {numberOfDays} days", exitCondition)
        
    let createCloseAfterFixedNumberOfDaysWithStop (stopDescription:string) (numberOfDays:int) (stopPrice:decimal) : ITradingStrategy =
        let daysHeldReached = fun (context:SimulationContext) (bar:PriceBar) -> bar.Date - context.Position.Opened > TimeSpan.FromDays(float numberOfDays)
        let stopReached = fun (context:SimulationContext) (bar:PriceBar) ->
            match context.Position.IsShort with
            | true -> bar.Close > stopPrice
            | false -> bar.Close < stopPrice
        let exitCondition = fun (context:SimulationContext) (bar:PriceBar) ->
            let doExit = daysHeldReached context bar || stopReached context bar
            (doExit, Some stopPrice)
        TradingStrategyCloseOnCondition($"Close after {numberOfDays} days with {stopDescription}", exitCondition)
        
    let createTrailingStop (stopDescription:string) (trailingPercentage:decimal) (initialStop:decimal option)  : ITradingStrategy =
        if trailingPercentage <= 0m then
            failwith "Trailing percentage must be greater than 0"
        if trailingPercentage >= 1m then
            failwith "Trailing percentage must be less than 1"
        if initialStop.IsSome && initialStop.Value < 0m then
            failwith "Initial stop must be greater or equal to 0"
        
        let latestStop =
            match initialStop with
            | Some stop -> ref stop
            | None -> ref 0m
            
        let stopReached = fun (context:SimulationContext) (bar:PriceBar) ->
            let stopReached =
                match context.Position.IsShort with
                | true -> bar.Close > latestStop.Value && latestStop.Value <> 0m
                | false -> bar.Close < latestStop.Value
            
            match stopReached with
            | true ->
                true, Some latestStop.Value
                
            | false ->
                match context.Position.IsShort with
                | true ->
                    let newStopPriceCandidate = bar.Close * (1m + trailingPercentage)
                    // the initial stop is 0m if one is not set explicitly
                    // and that breaks the math.min logic as that initial stop will always be selected
                    // so we need to handle that appropriately
                    latestStop.Value <-
                        match latestStop.Value with
                        | 0m -> newStopPriceCandidate
                        | _ -> Math.Min(latestStop.Value,newStopPriceCandidate)
                | false ->
                    let newStopPriceCandidate = bar.Close * (1m - trailingPercentage)
                    latestStop.Value <- Math.Max(latestStop.Value,newStopPriceCandidate)
                
                (false, Some latestStop.Value)
                
        TradingStrategyCloseOnCondition($"Trailing stop with {stopDescription}", stopReached)
        
    let createLastSellStrategy (position:StockPositionState) : ITradingStrategy =
        let exitCondition = fun (_:SimulationContext) (bar:PriceBar) ->
            let doExit = position.Closed.IsSome && bar.Date.Date = position.Closed.Value.Date
            doExit, None
        TradingStrategyCloseOnCondition("Close last sell", exitCondition)
    
    let getStrategies (actualTrade:StockPositionState option) : ITradingStrategy seq =
        let firstStop (position:StockPositionState) =
            let defaultStop =
                match position.StockPositionType with
                | Short -> Decimal.MaxValue
                | Long -> 0m
            position |> StockPositionWithCalculations |> _.FirstStop() |> Option.defaultValue defaultStop
            
        let percentStopBasedOnCostPerShare (percentage:decimal) (position:StockPositionState) =
            let costPerShare = position |> StockPositionWithCalculations |> _.CompletedPositionCostPerShare
            let multiplier =
                match position.StockPositionType with
                | Short -> 1m + percentage
                | Long -> 1m - percentage
            costPerShare * multiplier
            
        [
            createProfitPointsBasedOnPctGainTrade 0.07m 3
            createProfitPointsBasedOnPctGainTrade 0.10m 3
            createProfitPointsBasedOnPctGainTrade 0.20m 3
            createCloseAfterFixedNumberOfDays 15
            createCloseAfterFixedNumberOfDays 30
            createCloseAfterFixedNumberOfDays 60
            createCloseAfterFixedNumberOfDays 90
            createCloseAfterFixedNumberOfDays 120
            createTrailingStop "5%" 0.05m None
            createTrailingStop "10%" 0.10m None
            createTrailingStop "20%" 0.20m None
            if actualTrade.IsSome then
                createLastSellStrategy actualTrade.Value
                createCloseAfterFixedNumberOfDaysWithStop "first stop" 30 (actualTrade.Value |> firstStop)
                createCloseAfterFixedNumberOfDaysWithStop "5%" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.05m)
                createCloseAfterFixedNumberOfDaysWithStop "10%" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.10m)
                createCloseAfterFixedNumberOfDaysWithStop "20%" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.20m)
                createTrailingStop "5% w/ first stop" 0.05m (actualTrade.Value |> firstStop |> Some)
                createTrailingStop "10% w/ first stop" 0.10m (actualTrade.Value |> firstStop |> Some)
                createTrailingStop "20% w/ first stop" 0.20m (actualTrade.Value |> firstStop |> Some)
                
            // createCloseAfterFixedNumberOfDays 15 - retired, consistently underperforms
            // createProfitPointsTrade 3 - retired, consistently underperforms            
        ]
    
type TradingStrategyRunner(brokerage:IBrokerageGetPriceHistory, hours:IMarketHours, logger:ILogger) =
    
    let setRiskAmountFromActualTradeIfSet actualTrade date stockPosition =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            match actualTrade.RiskAmount with
            | Some riskAmount -> stockPosition |> StockPosition.setRiskAmount riskAmount date
            | None -> stockPosition
            
    let setLabelsFromActualTradeIfSet (actualTrade:StockPositionState option) date (stockPosition:StockPositionState) =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            actualTrade.Labels |> Seq.fold (fun position label -> position |> StockPosition.setLabel label.Key label.Value date) stockPosition
            
    let assignInitialNoteFromActualTradeIfSet (actualTrade:StockPositionState option) (stockPosition:StockPositionState) =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            match actualTrade.Notes with
            | [] -> stockPosition
            | _ -> stockPosition |> StockPosition.addNotes (Some (actualTrade.Notes[0].content)) actualTrade.Notes[0].created
            
    let assignGradeFromActualTradeIfSet (actualTrade:StockPositionState option) (stockPosition:StockPositionState) =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            match actualTrade.Grade with
            | Some grade -> stockPosition |> StockPosition.assignGrade grade actualTrade.GradeNote (actualTrade.Notes |> List.last |> _.created)
            | None -> stockPosition
    
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice,
            ticker:Ticker,
            ``when``:DateTimeOffset,
            closeIfOpenAtTheEnd:bool,
            actualTrade:StockPositionState option) =
        
        task {
            // when we simulate a purchase for that day, assume it's end of the day
            // so that the price feed will return data from that day and not the previous one
            let convertedWhen = hours.GetMarketEndOfDayTimeInUtc(``when``)
            
            let! prices =
                brokerage.GetPriceHistory
                    user
                    ticker
                    PriceFrequency.Daily
                    (Some convertedWhen)
                    (TradingStrategyConstants.MaxNumberOfDaysToSimulate |> convertedWhen.AddDays |> Some)
                    
            let results = TradingStrategyResults()
            
            match prices with
            | Error err -> results.MarkAsFailed($"Failed to get price history for {ticker}: {err.Message}")
            | Ok bars when bars.Bars.Length = 0 -> results.MarkAsFailed($"No price history for {ticker}")
            | Ok bars ->
                
                // purchase price can be very different from the bar price because
                // of stock splits and what not. So if we see a big difference, we should
                // use the bar open as purchase price. Use percent difference to determine
                // if the difference is big enough to warrant using the bar open price
                
                let purchasePrice =
                    let percentDiff = Math.Abs(price - bars.Bars[0].Open) / price
                    match percentDiff > 0.2m with
                    | true ->
                        logger.LogInformation($"Price difference for {ticker} is too big by {percentDiff:P}. Using bar open price instead of {price} for purchase")
                        bars.Bars[0].Open
                    | false -> price
                
                let stockPosition =
                    StockPosition.``open`` ticker numberOfShares purchasePrice ``when``
                    |> StockPosition.setStop stopPrice ``when``
                    |> setRiskAmountFromActualTradeIfSet actualTrade ``when``
                    |> setLabelsFromActualTradeIfSet actualTrade ``when``
                    |> assignInitialNoteFromActualTradeIfSet actualTrade
                    
                TradingStrategyFactory.getStrategies actualTrade
                    |> Seq.iter ( fun strategy ->
                        let result = stockPosition |> strategy.Run bars closeIfOpenAtTheEnd
                        
                        // dog and pony show to make sure we have the grade
                        let withGrade =
                            result.Position.GetPositionState()
                            |> assignGradeFromActualTradeIfSet actualTrade
                            |> StockPositionWithCalculations
                        
                        {result with Position = withGrade} |> results.Add
                    )
                    
                match actualTrade with
                | Some actualTrade ->
                    let actualResult = TradingStrategyFactory.createActualTrade().Run bars closeIfOpenAtTheEnd actualTrade
                    results.Insert(0, actualResult)
                | None -> ()
            
            return results
        }
        
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice,
            ticker:Ticker,
            ``when``:DateTimeOffset,
            closeIfOpenAtTheEnd:bool) =
        
        this.Run(
            user,
            numberOfShares,
            price,
            stopPrice,
            ticker,
            ``when``,
            closeIfOpenAtTheEnd,
            actualTrade = None)
        
    member this.Run(user:UserState, position:StockPositionState,closeIfOpenAtTheEnd) =
        
        let calculations = position |> StockPositionWithCalculations
        
        let stopPrice =
            match calculations.FirstStop() with
            | None -> Some (calculations.CompletedPositionCostPerShare * TradingStrategyConstants.DefaultStopPriceMultiplier)
            | Some _ -> calculations.FirstStop()
            
        let numberOfShares =
            match position.StockPositionType with
            | Short -> calculations.CompletedPositionShares * -1m
            | Long -> calculations.CompletedPositionShares
                
        this.Run(
            user=user,
            numberOfShares=numberOfShares,
            price=calculations.CompletedPositionCostPerShare,
            stopPrice = stopPrice,
            ticker = position.Ticker,
            ``when``=position.Opened,
            closeIfOpenAtTheEnd=closeIfOpenAtTheEnd,
            actualTrade = Some position
        )
