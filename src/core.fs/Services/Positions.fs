namespace core.fs.Services

open System
open core.Stocks
open core.fs.Services.Analysis
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Domain

module PositionAnalysis =
    
    module PortfolioAnalysisKeys =
        
        let PercentToStopLoss = "PercentToStopLoss"
        let DaysSinceOpened = "DaysSinceOpened"
        let RecentFilings = "RecentFilings"
        let GainPct = "GainPct"
        let AverageCost = "AverageCost"
        let StopLoss = "StopLoss"
        let RR = "RR"
        let Profit = "Profit"
        let UnrealizedProfit = "UnrealizedProfit"
        let UnrealizedGain = "UnrealizedGain"
        let Price = "Price"
        let RiskAmount = "RiskedAmount"
        let DaysSinceLastTransaction = "DaysSinceLastTransaction"
        let PositionSize = "PositionSize"
        let DaysHeld = "DaysHeld"
        let MaxDrawdown = "MaxDrawdown"
        let MaxGain = "MaxGain"
        let GainAndDrawdownDiff = "GainDiff"
        let MaxDrawdownLast10 = "MaxDrawdownLast10"
        let MaxGainLast10 = "MaxGainLast10"
        let GainDiffLast10 = "GainDiffLast10"
        let StrategyLabel = "StrategyLabel"
        let HasSellOrder = "HasSellOrder"

    let generate (position:StockPositionWithCalculations) (bars:PriceBars) orders =
        
        let stopLoss = 
            match position.StopPrice with
            | Some p -> p
            | None -> 0.0m
        
        let pctToStop =
            match stopLoss with
            | 0.0m -> -1.0m
            | _ -> (stopLoss - bars.Last.Close) / stopLoss
        
        let rrOutcomeType = 
            match position.RR with
            | p when p >= 1.0m -> OutcomeType.Positive
            | p when p < 0.0m -> OutcomeType.Negative
            | _ -> OutcomeType.Neutral
            
        let unrealizedGainPct = (bars.Last.Close - position.AverageCostPerShare) / position.AverageCostPerShare
        
        let max = bars.Bars |> Array.maxBy (fun (b:PriceBar) -> b.High) |> fun b -> b.High
        let gain = (max - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let min = bars.Bars |> Array.minBy (fun b -> b.Low) |> fun b -> b.Low
        let drawdown = (min - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let last10 = 10 |> bars.LatestOrAll
        let last10Max = last10.Bars |> Array.maxBy (fun b -> b.High) |> fun b -> b.High
        let last10Gain = (last10Max - last10.First.Close) / last10.First.Close
        
        let last10Min = last10.Bars |> Array.minBy (fun b -> b.Low) |> fun b -> b.Low
        let last10Drawdown = (last10Min - last10.First.Close) / last10.First.Close
        
        let last10MaxGainDrawdownDiff = last10Gain + last10Drawdown
        
        let daysSinceOpened = 
            position.Opened
            |> DateTimeOffset.UtcNow.Subtract
            |> fun ts -> ts.TotalDays
            |> Math.Ceiling
        
        let hasSellOrderInOrders = 
            orders
            |> Array.exists (fun (o:Order) -> o.IsSellOrder && o.Ticker.Value = position.Ticker)
            
        let unrealizedProfit =  position.Profit + position.NumberOfShares * (bars.Last.Close - position.AverageCostPerShare)
        
        [
            AnalysisOutcome(PortfolioAnalysisKeys.Price, OutcomeType.Neutral, bars.Last.Close, ValueFormat.Currency, $"Price: {bars.Last.Close:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.StopLoss, OutcomeType.Neutral, stopLoss, ValueFormat.Currency, $"Stop loss is {stopLoss:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.PercentToStopLoss, (if pctToStop < 0.0m then OutcomeType.Neutral else OutcomeType.Negative), pctToStop, ValueFormat.Percentage, $"%% difference to stop loss {stopLoss} is {pctToStop}")
            AnalysisOutcome(PortfolioAnalysisKeys.AverageCost, OutcomeType.Neutral, Math.Round(position.AverageCostPerShare, 2), ValueFormat.Currency, $"Average cost per share is {position.AverageCostPerShare:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainPct, (if position.GainPct >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.GainPct, ValueFormat.Percentage, $"{position.GainPct:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.RR, rrOutcomeType, Math.Round(position.RR, 2), ValueFormat.Number, $"{position.RR:N2}")
            AnalysisOutcome(PortfolioAnalysisKeys.Profit, (if position.Profit >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.Profit, ValueFormat.Currency, $"{position.Profit}")
            AnalysisOutcome(PortfolioAnalysisKeys.UnrealizedProfit, (if unrealizedProfit >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), unrealizedProfit, ValueFormat.Currency, $"{unrealizedProfit}")
            AnalysisOutcome(PortfolioAnalysisKeys.UnrealizedGain, (if unrealizedGainPct >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), unrealizedGainPct, ValueFormat.Percentage, $"{unrealizedGainPct:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxGain, OutcomeType.Neutral, gain, ValueFormat.Percentage, $"Max gain is {gain:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxDrawdown, OutcomeType.Neutral, drawdown, ValueFormat.Percentage, $"Max drawdown is {drawdown:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainAndDrawdownDiff, (if gain + drawdown >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), gain + drawdown, ValueFormat.Percentage, $"Max gain drawdown diff is {(gain - drawdown) * -1.0m:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxGainLast10, OutcomeType.Neutral, last10Gain, ValueFormat.Percentage, $"Max gain in last 10 bars is {last10Gain:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxDrawdownLast10, OutcomeType.Neutral, last10Drawdown, ValueFormat.Percentage, $"Max drawdown in last 10 bars is {last10Drawdown:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainDiffLast10, (if last10MaxGainDrawdownDiff >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), last10MaxGainDrawdownDiff, ValueFormat.Percentage, $"Max gain drawdown diff in last 10 bars is {last10MaxGainDrawdownDiff:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.RiskAmount, OutcomeType.Neutral, (if position.RiskedAmount.IsSome then position.RiskedAmount.Value else 0.0m), ValueFormat.Currency, $"Risk amount is {position.RiskedAmount:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysHeld, OutcomeType.Neutral, position.DaysHeld, ValueFormat.Number, $"Days held: {position.DaysHeld}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysSinceLastTransaction, OutcomeType.Neutral, position.DaysSinceLastTransaction, ValueFormat.Number, $"Last transaction was {position.DaysSinceLastTransaction} days ago")
            AnalysisOutcome(PortfolioAnalysisKeys.PositionSize, OutcomeType.Neutral, position.Cost, ValueFormat.Currency, $"Position size is {position.Cost}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysSinceOpened, OutcomeType.Neutral, decimal daysSinceOpened, ValueFormat.Number, $"Days since opened: {daysSinceOpened}")
            AnalysisOutcome(PortfolioAnalysisKeys.StrategyLabel, OutcomeType.Negative, (if position.ContainsLabel("strategy") then 1 else 0), ValueFormat.Boolean, $"Missing strategy label")
            AnalysisOutcome(PortfolioAnalysisKeys.HasSellOrder, OutcomeType.Neutral, (if hasSellOrderInOrders then 1 else 0), ValueFormat.Boolean, $"Has sell order")
        ]
        
    let evaluate (tickerOutcomes:seq<TickerOutcomes>) =
        
        let percentToStopThreshold = -0.02m
        let recentlyOpenThreshold = TimeSpan.FromDays(5)
        let withinTwoWeeksThreshold = TimeSpan.FromDays(14)
        
        [
            AnalysisOutcomeEvaluation(
                "Below stop loss",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.Profit,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PortfolioAnalysisKeys.PercentToStopLoss && o.Value > 0.0m) ]
            )
            AnalysisOutcomeEvaluation(
                "Stop loss at risk",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PortfolioAnalysisKeys.PercentToStopLoss && o.Value >= percentToStopThreshold && o.Value <= 0.0m) ]
            )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {recentlyOpenThreshold.TotalDays |> int} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.Profit,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PortfolioAnalysisKeys.DaysSinceOpened && o.Value <= decimal recentlyOpenThreshold.TotalDays) ]
            )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {withinTwoWeeksThreshold.TotalDays |> int} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PortfolioAnalysisKeys.DaysSinceOpened && o.Value <= decimal withinTwoWeeksThreshold.TotalDays) ]
            )
            AnalysisOutcomeEvaluation(
                "No Strategy",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.StrategyLabel,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PortfolioAnalysisKeys.StrategyLabel && o.Value = 0m) ]
            )
        ]

    let dailyPLAndGain (bars:PriceBars) (position:StockPositionWithCalculations) =
        
        let firstBar = 
            bars.Bars
            |> Array.findIndex (fun b -> b.Date >= position.Opened)
        
        let lastBar = 
            match position.Closed with
            | Some closed ->
                bars.Bars
                |> Array.findIndexBack (fun b -> b.Date <= closed)
            | None -> bars.Length - 1
        
        let shares = position.CompletedPositionShares
        let costBasis = position.AverageBuyCostPerShare
        
        let profit = ChartDataPointContainer<decimal>("Profit", DataPointChartType.Line)
        let gainPct = ChartDataPointContainer<decimal>("Gain %", DataPointChartType.Line)
        
        for i in firstBar..lastBar do
            let bar = bars.Bars[i]
            
            let currentPrice = bar.High
            let currentGainPct = (currentPrice - costBasis) / costBasis * 100.0m
            let currentProfit = shares * (currentPrice - costBasis)
            
            profit.Add(bar.Date, currentProfit)
            gainPct.Add(bar.Date, currentGainPct)
        
        (profit, gainPct)