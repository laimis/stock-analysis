namespace core.fs.Services.Trading

open System
open System.Collections.Generic
open core.fs
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
        Position:StockPositionState
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
        Position:StockPositionWithCalculations
        StrategyName:string
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
        let riskPerShare = position.CompletedPositionCostPerShare - position.FirstStop.Value
        position.CompletedPositionCostPerShare + riskPerShare * decimal(level)
     
    let getProfitPointWithPercentGain (level:int) (percentGain:decimal) (position:StockPositionWithCalculations) =
        let singleLevel = position.CompletedPositionCostPerShare * percentGain
        position.CompletedPositionCostPerShare + singleLevel * decimal(level)
    
    
    let getProfitPointsWithStopPrice levels position =
        let profitPoints =
            [1..levels]
            |> List.map (fun l -> position |> getProfitPointWithStopPrice l)
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
        WinRRTotal:decimal
        Losses:int
        LossAmount:decimal
        MaxLossAmount:decimal
        LossReturnPctTotal:decimal
        LossMaxReturnPct:decimal
        LossRRTotal:decimal
        TotalDaysHeldLosses:decimal
        TotalDaysHeld:decimal
        rrSum:decimal
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
                | _ ->
                    let winRatio =
                        match this.Wins with
                        | 0 -> 0m
                        | _ -> this.WinAmount / decimal this.Wins
                        
                    let lossRatio =
                        match this.Losses with
                        | 0 -> 0m
                        | _ -> this.LossAmount / decimal this.Losses
                        
                    this.WinPct * winRatio - (1m - this.WinPct) * lossRatio
                
            member this.WinAvgRR =
                match this.Wins with
                | 0 -> 0m
                | _ -> this.WinRRTotal / decimal this.Wins
                
            member this.LossAvgRR =
                match this.Losses with
                | 0 -> 0m
                | _ -> this.LossRRTotal / decimal this.Losses
                
            member this.rrRatio =
                match (this.WinAvgRR, this.LossAvgRR) with
                | 0m, _ -> this.LossAvgRR
                | _, 0m -> this.WinAvgRR
                | _ -> this.WinAvgRR / this.LossAvgRR |> Math.Abs
                
            member this.AvgReturnPct =
                match this.NumberOfTrades with
                | 0 -> 0m
                | _ -> this.Profit / this.TotalCost
                
            static member Create(closedPositions:seq<StockPositionWithCalculations>) =
                
                closedPositions
                |> Seq.fold (fun perf position ->
                    {   
                        NumberOfTrades = perf.NumberOfTrades + 1
                        TotalDaysHeld = perf.TotalDaysHeld + decimal(position.DaysHeld)
                        Profit = perf.Profit + position.Profit
                        TotalCost = perf.TotalCost + (if position.Cost <> 0m then position.Cost else position.CompletedPositionCost)
                        rrSum = perf.rrSum + position.RR
                        EarliestDate = if position.Opened < perf.EarliestDate then position.Opened else perf.EarliestDate
                        LatestDate = if position.Closed.IsSome && position.Closed.Value > perf.LatestDate then position.Closed.Value else perf.LatestDate
                        GradeDistribution = 
                            match position.Grade with
                            | Some grade ->
                                let gradeDistribution =
                                    match perf.GradeDistribution |> Array.tryFind (fun g -> g.label = grade.Value) with
                                    | Some _ -> perf.GradeDistribution |> Array.map (fun g -> if g.label = grade.Value then { g with frequency = g.frequency + 1 } else g)
                                    | None -> perf.GradeDistribution |> Array.append [| { label = grade.Value; frequency = 1 } |]
                                gradeDistribution
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
                    TotalCost = 0m
                    TotalDaysHeldWins = 0m
                    TotalDaysHeldLosses = 0m 
                    WinReturnPctTotal = 0m
                    LossReturnPctTotal = 0m
                    WinRRTotal = 0m
                    LossRRTotal = 0m 
                }

type TradingStrategyPerformance =
    {
        strategyName : string
        performance: TradingPerformance
        positions: StockPositionWithCalculations[]
    }
    
    with
        member this.NumberOfOpenPositions =
            this.positions
            |> Array.filter (fun p -> p.IsClosed = false)
            |> Array.length
