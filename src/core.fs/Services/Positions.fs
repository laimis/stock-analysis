namespace core.fs.Services

open System
open core.Shared
open core.Stocks
open core.fs.Services.Analysis
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks

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

    let generate (position:PositionInstance) bars orders =
        
        let stopLoss = 
            match position.StopPrice.HasValue with
            | true -> position.StopPrice.Value
            | false -> 0.0m
        
        let pctToStop = 
            match position.PercentToStop.HasValue with
            | true -> position.PercentToStop.Value
            | false -> 0.0m
        
        let rrOutcomeType = 
            match position.RR with
            | p when p >= 1.0m -> OutcomeType.Positive
            | p when p < 0.0m -> OutcomeType.Negative
            | _ -> OutcomeType.Neutral
        
        let max = bars |> Array.maxBy (fun (b:PriceBar) -> b.High) |> fun b -> b.High
        let gain = (max - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let min = bars |> Array.minBy (fun b -> b.Low) |> fun b -> b.Low
        let drawdown = (min - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let last10 = if bars.Length > 10 then bars[0..9] else bars
        let last10Max = last10 |> Array.maxBy (fun b -> b.High) |> fun b -> b.High
        let last10Gain = (last10Max - last10[0].Close) / last10[0].Close
        
        let last10Min = last10 |> Array.minBy (fun b -> b.Low) |> fun b -> b.Low
        let last10Drawdown = (last10Min - last10[0].Close) / last10[0].Close
        
        let last10MaxGainDrawdownDiff = last10Gain - last10Drawdown * -1.0m
        
        let daysSinceOpened = 
            position.Opened
            |> DateTimeOffset.UtcNow.Subtract
            |> fun ts -> ts.TotalDays
            |> Math.Ceiling
        
        let hasSellOrderInOrders = 
            orders
            |> Array.exists (fun (o:Order) -> o.IsSellOrder && o.Ticker.Value = position.Ticker)
        
        [
            AnalysisOutcome(PortfolioAnalysisKeys.Price, OutcomeType.Neutral, position.Price.Value, ValueFormat.Currency, $"Price: {position.Price.Value:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.StopLoss, OutcomeType.Neutral, stopLoss, ValueFormat.Currency, $"Stop loss is {stopLoss:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.PercentToStopLoss, (if pctToStop < 0.0m then OutcomeType.Neutral else OutcomeType.Negative), pctToStop, ValueFormat.Percentage, $"%% difference to stop loss {stopLoss} is {pctToStop}")
            AnalysisOutcome(PortfolioAnalysisKeys.AverageCost, OutcomeType.Neutral, Math.Round(position.AverageCostPerShare, 2), ValueFormat.Currency, $"Average cost per share is {position.AverageCostPerShare:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainPct, (if position.GainPct >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.GainPct, ValueFormat.Percentage, $"{position.GainPct:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.RR, rrOutcomeType, Math.Round(position.RR, 2), ValueFormat.Number, $"{position.RR:N2}")
            AnalysisOutcome(PortfolioAnalysisKeys.Profit, (if position.CombinedProfit >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.CombinedProfit, ValueFormat.Currency, $"{position.CombinedProfit}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxGain, OutcomeType.Neutral, gain, ValueFormat.Percentage, $"Max gain is {gain:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxDrawdown, OutcomeType.Neutral, drawdown, ValueFormat.Percentage, $"Max drawdown is {drawdown:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainAndDrawdownDiff, (if (gain - drawdown) * -1.0m >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), (gain - drawdown) * -1.0m, ValueFormat.Percentage, $"Max gain drawdown diff is {(gain - drawdown) * -1.0m:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxGainLast10, OutcomeType.Neutral, last10Gain, ValueFormat.Percentage, $"Max gain in last 10 bars is {last10Gain:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.MaxDrawdownLast10, OutcomeType.Neutral, last10Drawdown, ValueFormat.Percentage, $"Max drawdown in last 10 bars is {last10Drawdown:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.GainDiffLast10, (if last10MaxGainDrawdownDiff >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), last10MaxGainDrawdownDiff, ValueFormat.Percentage, $"Max gain drawdown diff in last 10 bars is {last10MaxGainDrawdownDiff:P}")
            AnalysisOutcome(PortfolioAnalysisKeys.RiskAmount, OutcomeType.Neutral, (if position.RiskedAmount.HasValue then position.RiskedAmount.Value else 0.0m), ValueFormat.Currency, $"Risk amount is {position.RiskedAmount:C2}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysHeld, OutcomeType.Neutral, position.DaysHeld, ValueFormat.Number, $"Days held: {position.DaysHeld}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysSinceLastTransaction, OutcomeType.Neutral, position.DaysSinceLastTransaction, ValueFormat.Number, $"Last transaction was {position.DaysSinceLastTransaction} days ago")
            AnalysisOutcome(PortfolioAnalysisKeys.PositionSize, OutcomeType.Neutral, position.Cost, ValueFormat.Currency, $"Position size is {position.Cost}")
            AnalysisOutcome(PortfolioAnalysisKeys.DaysSinceOpened, OutcomeType.Neutral, decimal daysSinceOpened, ValueFormat.Number, $"Days since opened: {daysSinceOpened}")
            AnalysisOutcome(PortfolioAnalysisKeys.StrategyLabel, OutcomeType.Negative, (if position.ContainsLabel("strategy") then 1 else 0), ValueFormat.Boolean, $"Missing strategy label")
            AnalysisOutcome(PortfolioAnalysisKeys.HasSellOrder, OutcomeType.Neutral, (if hasSellOrderInOrders then 1 else 0), ValueFormat.Boolean, $"Has sell order")
        ]
        
    let evaluate (tickerOutcomes:seq<TickerOutcomes>) =
        
        let percentToStopThreshold = -0.02m
        let recentlyOpenThreshold = 5m
        let withinTwoWeeksThreshold = 14m
        
        [
            AnalysisOutcomeEvaluation(
                "Below stop loss",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes
                    |> Seq.filter (fun t -> t.outcomes |> Seq.exists (fun o -> o.Key = PortfolioAnalysisKeys.PercentToStopLoss && o.Value > 0.0m))
                    |> Seq.toList
            )
            AnalysisOutcomeEvaluation(
                "Stop loss at risk",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.PercentToStopLoss,
                tickerOutcomes
                    |> Seq.filter (fun t -> t.outcomes |> Seq.exists (fun o -> o.Key = PortfolioAnalysisKeys.PercentToStopLoss && o.Value >= percentToStopThreshold && o.Value <= 0.0m))
                    |> Seq.toList
            )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {recentlyOpenThreshold} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes
                    |> Seq.filter (fun t -> t.outcomes |> Seq.exists (fun o -> o.Key = PortfolioAnalysisKeys.DaysSinceOpened && o.Value <= recentlyOpenThreshold))
                    |> Seq.toList
            )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {withinTwoWeeksThreshold} days",
                OutcomeType.Neutral,
                PortfolioAnalysisKeys.DaysSinceOpened,
                tickerOutcomes
                    |> Seq.filter (fun t -> t.outcomes |> Seq.exists (fun o -> o.Key = PortfolioAnalysisKeys.DaysSinceOpened && o.Value <= withinTwoWeeksThreshold))
                    |> Seq.toList
            )
            AnalysisOutcomeEvaluation(
                "No Strategy",
                OutcomeType.Negative,
                PortfolioAnalysisKeys.StrategyLabel,
                tickerOutcomes
                    |> Seq.filter (fun t -> t.outcomes |> Seq.exists (fun o -> o.Key = PortfolioAnalysisKeys.StrategyLabel && o.Value = 0m))
                    |> Seq.toList
            )
        ]

    let dailyPLAndGain (bars:PriceBar[]) (position:PositionInstance) =
        
        let firstBar = 
            bars
            |> Array.findIndex (fun b -> b.Date >= position.Opened)
        
        let lastBar = 
            match position.Closed.HasValue with
            | true ->
                bars
                |> Array.findIndex (fun b -> b.Date <= position.Closed.Value)
            | false -> bars.Length - 1
        
        let shares = position.CompletedPositionShares
        let costBasis = position.AverageBuyCostPerShare
        
        let profit = ChartDataPointContainer<decimal>("Profit", DataPointChartType.Line)
        let gainPct = ChartDataPointContainer<decimal>("Gain %", DataPointChartType.Line)
        
        for i in firstBar..lastBar do
            let bar = bars[i]
            
            let currentPrice = bar.High
            let currentGainPct = (currentPrice - costBasis) / costBasis * 100.0m
            let currentProfit = shares * (currentPrice - costBasis)
            
            profit.Add(bar.Date, currentProfit)
            gainPct.Add(bar.Date, currentGainPct)
        
        (profit, gainPct)