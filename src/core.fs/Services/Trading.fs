namespace core.fs.Services.Trading

open System
open System.Collections.Generic
open core.Stocks
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

module TradingStrategyConstants =
    // TODO: this needs to come from the environment or user settings
    let AvgPercentGain = 0.07m
    let DefaultStopPriceMultiplier = 0.95m
    let MaxNumberOfDaysToSimulate = 365
    let ActualTradesName = "Actual trades ⭐";
    
type SimulationContext =
    {
        Position:PositionInstance
        MaxGain:decimal
        MaxDrawdown:decimal
        Last10Bars:List<PriceBar>
    }

type TradingStrategyResult =
    {
        MaxDrawdownPct:decimal
        MaxGainPct:decimal
        MaxDrawdownPctRecent:decimal
        MaxGainPctRecent:decimal
        Position:PositionInstance
        StrategyName:string
    }

type TradingStrategyResults() =
    
    member val Results = List<TradingStrategyResult>() with get, set
    
    member this.Add(result:TradingStrategyResult) =
        this.Results.Add(result)
        
    member this.Insert(index:int, result:TradingStrategyResult) =
        this.Results.Insert(index, result)
        
    member val FailedReason:string option = None with get, set
    
    member this.Failed = this.FailedReason.IsNone
    
    member this.MarkAsFailed(reason:string) =
        this.FailedReason <- Some reason

type ITradingStrategy =
    abstract Run : position:PositionInstance -> bars:seq<PriceBar> -> TradingStrategyResult
        
module ProfitPoints =
    
    type ProfitPointContainer(name:string, prices:decimal seq) =
        member this.Name = name
        member this.Prices = prices
        
    let getProfitPointWithStopPrice (position:PositionInstance) (level:int) =
        let riskPerShare = position.CompletedPositionCostPerShare - position.FirstStop.Value
        position.CompletedPositionCostPerShare + riskPerShare * decimal(level)
     
    let getProfitPointWithPercentGain (position:PositionInstance) (level:int) (percentGain:decimal) =
        let singleLevel = position.CompletedPositionCostPerShare * percentGain
        position.CompletedPositionCostPerShare + singleLevel * decimal(level)
    
    
    let getProfitPointsWithStopPrice(position:PositionInstance, levels:int) =
        let profitPoints =
            [1..levels]
            |> List.map (getProfitPointWithStopPrice position)
            |> List.filter (fun p -> p > position.CompletedPositionCostPerShare)
            
        profitPoints
    
    let getProfitPoints func levels =
        [1..levels]
        |> List.map (fun level -> func level)
        

type TradingPerformance =
    {
        NumberOfTrades:int
        Wins:int
        WinAmount:decimal
        MaxWinAmount:decimal
        TotalDaysHeldWins:decimal
        Profit:decimal
        WinReturnPctTotal:decimal
        WinMaxReturnPct:decimal
        Losses:int
        LossAmount:decimal
        MaxLossAmount:decimal
        LossAvgReturnPct:decimal
        LossMaxReturnPct:decimal
        LossAvgDaysHeld:double
        WinPct:decimal
        EV:decimal
        AvgReturnPct:decimal
        TotalDaysHeld:decimal
        rrSum:decimal
        rrRatio:decimal
        EarliestDate:DateTimeOffset
        LatestDate:DateTimeOffset
        GradeDistribution:LabelWithFrequency[]
        TotalCost:decimal
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
        
            member this.WinAvgDaysHeld =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.TotalDaysHeldWins / decimal(this.Wins)
              
            member this.ReturnPctRatio =
                match this.LossAvgReturnPct with
                | 0m -> 0m
                | _ -> this.WinAvgReturnPct / this.LossAvgReturnPct
                
            member this.ProfitRatio =
                match this.AvgLossAmount with
                | 0m -> 0m
                | _ -> this.AvgWinAmount / this.AvgLossAmount
                
            member this.AverageDaysHeld =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.TotalDaysHeld / decimal(this.NumberOfTrades)
                
                
            static member Create(closedPositions:seq<PositionInstance>) =
                
                closedPositions
                |> Seq.fold (fun perf position ->
                    {
                        perf with 
                            NumberOfTrades = perf.NumberOfTrades + 1
                            TotalDaysHeld = perf.TotalDaysHeld + decimal(position.DaysHeld)
                            Profit = perf.Profit + position.Profit
                            TotalCost = perf.TotalCost + (if position.Cost <> 0m then position.Cost else position.CompletedPositionCost)
                            rrSum = perf.rrSum + position.RR
                            EarliestDate = if position.Opened < perf.EarliestDate then position.Opened else perf.EarliestDate
                            LatestDate = if position.Closed.HasValue && position.Closed.Value > perf.LatestDate then position.Closed.Value else perf.LatestDate
                            GradeDistribution = 
                                match position.Grade.HasValue with
                                | true ->
                                    let gradeDistribution =
                                        match perf.GradeDistribution |> Array.tryFind (fun g -> g.label = position.Grade.Value.Value) with
                                        | Some _ -> perf.GradeDistribution |> Array.map (fun g -> if g.label = position.Grade.Value.Value then { g with frequency = g.frequency + 1 } else g)
                                        | None -> perf.GradeDistribution |> Array.append [| { label = position.Grade.Value.Value; frequency = 1 } |]
                                    gradeDistribution
                                | false -> perf.GradeDistribution
                                
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
                                
                            WinReturnPctTotal =
                                match position.Profit >= 0m with
                                | true -> perf.WinReturnPctTotal + position.GainPct
                                | false -> perf.WinReturnPctTotal
                                
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
                    LossAvgReturnPct = 0m
                    LossMaxReturnPct = 0m
                    LossAvgDaysHeld = 0.0
                    WinPct = 0m
                    EV = 0m
                    AvgReturnPct = 0m
                    TotalDaysHeld = 0m
                    rrSum = 0m
                    rrRatio = 0m
                    EarliestDate = DateTimeOffset.MaxValue
                    LatestDate = DateTimeOffset.MinValue
                    GradeDistribution = [||]
                    TotalCost = 0m
                    TotalDaysHeldWins = 0m
                    WinReturnPctTotal = 0m 
                }

type TradingStrategyPerformance =
    {
        strategyName : string
        performance: TradingPerformance
        positions: PositionInstance[]
    }
    
    with
        member this.NumberOfOpenPositions =
            this.positions
            |> Array.filter (fun p -> p.Closed.HasValue)
            |> Array.length
