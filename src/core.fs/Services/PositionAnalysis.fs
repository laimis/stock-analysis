namespace core.fs.Services

open System
open System.Collections.Generic
open core.Account
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Stocks

module PositionAnalysis =
    
    module PositionAnalysisKeys =
        
        let PercentToStopLoss = "PercentToStopLoss"
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
        let StopDiffToSMA20Pct = "StopDiffToSMA20Pct"
        let StopDiffToCost = "StopDiffToCost"
        let PriceAboveEMA20Bars = "PriceAboveEMA20Bars"
        let EMA20AboveSMA50Bars = "EMA20AboveSMA50Bars"
        let SMA50Above200Bars = "SMA50Above200Bars"
        
    let generate (position:StockPositionWithCalculations) (bars:PriceBars) orders =
        
        let stopLoss = 
            match position.StopPrice with
            | Some p -> p
            | None -> 0.0m
        
        let pctToStop = position.PercentToStop bars.Last.Close
            
        let rrOutcomeType = 
            match position.RR with
            | p when p >= 1.0m -> OutcomeType.Positive
            | p when p < 0.0m -> OutcomeType.Negative
            | _ -> OutcomeType.Neutral
            
        let barAtOpen = bars.TryFindByDate position.Opened
        let barsFromOpen =
            match barAtOpen with
            | Some (index,_) -> bars.Bars[index..]
            | None ->
                // position might be opened before the first bar in the dataset
                match position.Opened with
                | x when x < bars.Bars[0].Date -> bars.Bars
                | _ -> failwith $"Unable to find start bar for {position.Ticker} on {position.Opened}, bars count: {bars.Length}"
                
        let max = barsFromOpen |> Array.maxBy (fun (b:PriceBar) -> b.High) |> _.High
        let gain = (max - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let min = barsFromOpen |> Array.minBy (_.Low) |> _.Low
        let drawdown = (min - position.CompletedPositionCostPerShare) / position.CompletedPositionCostPerShare
        
        let last10 = barsFromOpen[^9..]
        let last10Max = last10 |> Array.maxBy _.High |> _.High
        let last10Gain = (last10Max - last10[0].Close) / last10[0].Close
        
        let last10Min = last10 |> Array.minBy _.Low |> _.Low
        let last10Drawdown = (last10Min - last10[0].Close) / last10[0].Close
        
        let last10MaxGainDrawdownDiff = last10Gain + last10Drawdown
        
        let hasSellOrderInOrders = 
            orders
            |> Array.exists (fun (o:StockOrder) -> o.IsSellOrder && o.Ticker = position.Ticker)
            
        let unrealizedProfit =  position.Profit + position.NumberOfShares * (bars.Last.Close - position.AverageCostPerShare)
        let unrealizedGainPct = (bars.Last.Close - position.AverageCostPerShare) / position.AverageCostPerShare
        
        let multipleBarOutcomes = MultipleBarPriceAnalysis.run bars
        let sma20 = multipleBarOutcomes |> Seq.find (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage(20)) |> _.Value
        let stopDiffToSMA20Pct =
            match sma20 with
            | x when x = 0m -> 0.0m
            | _ -> (stopLoss - sma20) / sma20
            
        let stopDiffToCost =
            match position.StopPrice with
            | None -> 0m
            | Some stopLoss -> (stopLoss - position.AverageCostPerShare) / position.AverageCostPerShare
            
        let priceAboveEMA20Bars = multipleBarOutcomes |> Seq.tryFind (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAboveEMA20Bars) |> Option.map _.Value |> Option.defaultValue 0m
        let ema20overSMA50 = multipleBarOutcomes |> Seq.tryFind (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EMA20AboveSMA50Bars) |> Option.map _.Value |> Option.defaultValue 0m
        let sma50overSMA200 = multipleBarOutcomes |> Seq.tryFind (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA50AboveSMA200Bars) |> Option.map _.Value |> Option.defaultValue 0m
        
        [
            AnalysisOutcome(PositionAnalysisKeys.Price, OutcomeType.Neutral, bars.Last.Close, ValueFormat.Currency, $"Price: {bars.Last.Close:C2}")
            AnalysisOutcome(PositionAnalysisKeys.StopLoss, OutcomeType.Neutral, stopLoss, ValueFormat.Currency, $"Stop loss is {stopLoss:C2}")
            AnalysisOutcome(PositionAnalysisKeys.AverageCost, OutcomeType.Neutral, Math.Round(position.AverageCostPerShare, 2), ValueFormat.Currency, $"Average cost per share is {position.AverageCostPerShare:C2}")
            AnalysisOutcome(PositionAnalysisKeys.PercentToStopLoss, (if pctToStop < 0.0m then OutcomeType.Neutral else OutcomeType.Negative), pctToStop, ValueFormat.Percentage, $"%% difference to stop loss {stopLoss} is {pctToStop}")
            AnalysisOutcome(PositionAnalysisKeys.GainPct, (if position.GainPct >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.GainPct, ValueFormat.Percentage, $"{position.GainPct:P}")
            AnalysisOutcome(PositionAnalysisKeys.RR, rrOutcomeType, Math.Round(position.RR, 2), ValueFormat.Number, $"{position.RR:N2}")
            AnalysisOutcome(PositionAnalysisKeys.Profit, (if position.Profit >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), position.Profit, ValueFormat.Currency, $"{position.Profit}")
            AnalysisOutcome(PositionAnalysisKeys.UnrealizedProfit, (if unrealizedProfit >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), unrealizedProfit, ValueFormat.Currency, $"{unrealizedProfit}")
            AnalysisOutcome(PositionAnalysisKeys.UnrealizedGain, (if unrealizedGainPct >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), unrealizedGainPct, ValueFormat.Percentage, $"{unrealizedGainPct:P}")
            AnalysisOutcome(PositionAnalysisKeys.RiskAmount, OutcomeType.Neutral, (if position.RiskedAmount.IsSome then position.RiskedAmount.Value else 0.0m), ValueFormat.Currency, $"Risk amount is {position.RiskedAmount:C2}")
            AnalysisOutcome(PositionAnalysisKeys.DaysHeld, OutcomeType.Neutral, position.DaysHeld, ValueFormat.Number, $"Days held: {position.DaysHeld}")
            AnalysisOutcome(PositionAnalysisKeys.DaysSinceLastTransaction, OutcomeType.Neutral, position.DaysSinceLastTransaction, ValueFormat.Number, $"Last transaction was {position.DaysSinceLastTransaction} days ago")
            AnalysisOutcome(PositionAnalysisKeys.PositionSize, OutcomeType.Neutral, position.Cost, ValueFormat.Currency, $"Position size is {position.Cost}")
            AnalysisOutcome(PositionAnalysisKeys.StopDiffToSMA20Pct, OutcomeType.Neutral, stopDiffToSMA20Pct, ValueFormat.Percentage, $"Stop diff to SMA20 is {stopDiffToSMA20Pct:P}")
            AnalysisOutcome(PositionAnalysisKeys.StopDiffToCost, OutcomeType.Neutral, stopDiffToCost, ValueFormat.Percentage, $"Stop diff to cost is {stopDiffToCost:P}")
            AnalysisOutcome(PositionAnalysisKeys.StrategyLabel, OutcomeType.Negative, (if position.ContainsLabel("strategy") then 1 else 0), ValueFormat.Boolean, $"Missing strategy label")
            AnalysisOutcome(PositionAnalysisKeys.HasSellOrder, OutcomeType.Neutral, (if hasSellOrderInOrders then 1 else 0), ValueFormat.Boolean, $"Has sell order")
            AnalysisOutcome(PositionAnalysisKeys.MaxGain, OutcomeType.Neutral, gain, ValueFormat.Percentage, $"Max gain is {gain:P}")
            AnalysisOutcome(PositionAnalysisKeys.MaxDrawdown, OutcomeType.Neutral, drawdown, ValueFormat.Percentage, $"Max drawdown is {drawdown:P}")
            AnalysisOutcome(PositionAnalysisKeys.GainAndDrawdownDiff, (if gain + drawdown >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), gain + drawdown, ValueFormat.Percentage, $"Max gain drawdown diff is {(gain - drawdown) * -1.0m:P}")
            AnalysisOutcome(PositionAnalysisKeys.MaxGainLast10, OutcomeType.Neutral, last10Gain, ValueFormat.Percentage, $"Max gain in last 10 bars is {last10Gain:P}")
            AnalysisOutcome(PositionAnalysisKeys.MaxDrawdownLast10, OutcomeType.Neutral, last10Drawdown, ValueFormat.Percentage, $"Max drawdown in last 10 bars is {last10Drawdown:P}")
            AnalysisOutcome(PositionAnalysisKeys.GainDiffLast10, (if last10MaxGainDrawdownDiff >= 0.0m then OutcomeType.Positive else OutcomeType.Negative), last10MaxGainDrawdownDiff, ValueFormat.Percentage, $"Max gain drawdown diff in last 10 bars is {last10MaxGainDrawdownDiff:P}")
            AnalysisOutcome(PositionAnalysisKeys.PriceAboveEMA20Bars, OutcomeType.Neutral, priceAboveEMA20Bars, ValueFormat.Number, $"Price above EMA20 bars: {priceAboveEMA20Bars}")
            AnalysisOutcome(PositionAnalysisKeys.EMA20AboveSMA50Bars, OutcomeType.Neutral, ema20overSMA50, ValueFormat.Number, $"EMA20 over SMA50 bars: {ema20overSMA50}")
            AnalysisOutcome(PositionAnalysisKeys.SMA50Above200Bars, OutcomeType.Neutral, sma50overSMA200, ValueFormat.Number, $"SMA50 over SMA200 bars: {sma50overSMA200}")
        ]
        
    let evaluate (_:UserState) (tickerOutcomes:seq<TickerOutcomes>) =
        
        // NOTE: would be nice to have these come from user configuration
        let percentToStopThreshold = -0.02m
        let recentlyOpenThreshold = TimeSpan.FromDays(5)
        let withinTwoWeeksThreshold = TimeSpan.FromDays(14)
        let gainPctThreshold = 0.07m
        
        let shortsInUptrendFilter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.EMA20AboveSMA50Bars && o.Value > 0.0m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value < 0.0m)
        ]
        
        let longsInDowntrendFilter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.EMA20AboveSMA50Bars && o.Value < 0.0m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value > 0.0m)
        ]
        
        let longsWentAboveEMA20Filter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.PriceAboveEMA20Bars && o.Value = 1m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value > 0.0m)
        ]
        
        let longsWentUnderEMA20Filter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.PriceAboveEMA20Bars && o.Value = -1m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value > 0.0m)
        ]
        
        let shortsWentBelowEMA20Filter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.PriceAboveEMA20Bars && o.Value = -1m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value < 0.0m)
        ]
        
        let shortsWentAboveEMA20Filter = [
            (fun (o:AnalysisOutcome) -> o.Key = PositionAnalysisKeys.PriceAboveEMA20Bars && o.Value = 1m)
            (fun o -> o.Key = PositionAnalysisKeys.PositionSize && o.Value < 0.0m)
        ]
        
        let daysHeldIntervals = [22m; 30m]
        
        [
            AnalysisOutcomeEvaluation(
                "Below stop loss",
                OutcomeType.Negative,
                PositionAnalysisKeys.StopLoss,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.PercentToStopLoss && o.Value > 0.0m) ]
            )
            AnalysisOutcomeEvaluation(
                "Stop loss at risk",
                OutcomeType.Neutral,
                PositionAnalysisKeys.PercentToStopLoss,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.PercentToStopLoss && o.Value >= percentToStopThreshold && o.Value <= 0.0m) ]
            )
            yield!
                daysHeldIntervals
                |> List.map(fun i ->
                    AnalysisOutcomeEvaluation(
                        $"Opened {i} days ago",
                        OutcomeType.Neutral,
                        PositionAnalysisKeys.DaysHeld,
                        tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.DaysHeld && o.Value = i) ]
                    )
                )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {recentlyOpenThreshold.TotalDays |> int} days",
                OutcomeType.Neutral,
                PositionAnalysisKeys.UnrealizedProfit,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.DaysHeld && o.Value <= decimal recentlyOpenThreshold.TotalDays) ]
            )
            AnalysisOutcomeEvaluation(
                $"Opened in the last {withinTwoWeeksThreshold.TotalDays |> int} days",
                OutcomeType.Neutral,
                PositionAnalysisKeys.UnrealizedProfit,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.DaysHeld && o.Value <= decimal withinTwoWeeksThreshold.TotalDays) ]
            )
            AnalysisOutcomeEvaluation(
                "No Strategy",
                OutcomeType.Negative,
                PositionAnalysisKeys.StrategyLabel,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PositionAnalysisKeys.StrategyLabel && o.Value = 0m) ]
            )
            AnalysisOutcomeEvaluation(
                "No Risk Amount",
                OutcomeType.Negative,
                PositionAnalysisKeys.RiskAmount,
                tickerOutcomes |> TickerOutcomes.filter [
                    (fun o -> o.Key = PositionAnalysisKeys.RiskAmount && o.Value = 0m)
                    (fun o -> o.Key = PositionAnalysisKeys.StopLoss && o.Value > 0m)
                ]
            )
            AnalysisOutcomeEvaluation(
                "Shorts in Uptrend",
                OutcomeType.Negative,
                PositionAnalysisKeys.EMA20AboveSMA50Bars,
                tickerOutcomes |> TickerOutcomes.filter [
                    yield! shortsInUptrendFilter
                ]
            )
            AnalysisOutcomeEvaluation(
                "EMA20 < SMA50",
                OutcomeType.Negative,
                PositionAnalysisKeys.EMA20AboveSMA50Bars,
                tickerOutcomes |> TickerOutcomes.filter [
                    yield! longsInDowntrendFilter
                ]
            )
            AnalysisOutcomeEvaluation(
                "Avoid Sells",
                OutcomeType.Neutral,
                PositionAnalysisKeys.DaysHeld,
                tickerOutcomes |> TickerOutcomes.filter [
                    (fun o -> o.Key = PositionAnalysisKeys.DaysHeld && o.Value <= decimal recentlyOpenThreshold.TotalDays)
                    (fun o -> o.Key = PositionAnalysisKeys.UnrealizedProfit && o.Value > 0m)
                ]
            )
            AnalysisOutcomeEvaluation(
                "Recent but negative profit",
                OutcomeType.Negative,
                PositionAnalysisKeys.UnrealizedProfit,
                tickerOutcomes |> TickerOutcomes.filter [
                    (fun o -> o.Key = PositionAnalysisKeys.DaysHeld && o.Value <= decimal withinTwoWeeksThreshold.TotalDays)
                    (fun o -> o.Key = PositionAnalysisKeys.UnrealizedProfit && o.Value < 0m)
                ]
            )
            AnalysisOutcomeEvaluation(
                "Longs went above EMA20",
                OutcomeType.Positive,
                PositionAnalysisKeys.PriceAboveEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter longsWentAboveEMA20Filter
            )
            AnalysisOutcomeEvaluation(
                "Longs went under EMA20",
                OutcomeType.Negative,
                PositionAnalysisKeys.PriceAboveEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter longsWentUnderEMA20Filter
            )
            AnalysisOutcomeEvaluation(
                "Shorts went below EMA20",
                OutcomeType.Positive,
                PositionAnalysisKeys.PriceAboveEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter shortsWentBelowEMA20Filter
            )
            AnalysisOutcomeEvaluation(
                "Shorts went above EMA20",
                OutcomeType.Negative,
                PositionAnalysisKeys.PriceAboveEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter shortsWentAboveEMA20Filter
            )
        ]

    let correlations (matrix:PriceBar array array) =
        // expect all the arrays to be the same length as the first one
        // if you find an array that doesn't match the first one's length
        // either add elements to it or remove elements from it
        // but then replace the results with all zeros because we want to
        // somehow indicate that the data is not valid
        
        let matrixOfChanges =
            matrix
            |> Array.map (
                fun row ->
                    row
                    |> Array.map (fun bar -> bar.Close |> float)
                    |> Array.pairwise
                    |> Array.map (fun (prev, current) -> (current - prev) / prev)
            )
            
        let expectedLength = matrixOfChanges[0].Length
        
        let invalidIndexes =
            matrixOfChanges
            |> Array.mapi (fun i row -> i, row.Length)
            |> Array.filter (fun (_, length) -> length <> expectedLength) |> Array.map fst |> HashSet
            
        let matrixToSend =
            matrixOfChanges
            |> Array.map (
                fun row ->
                    match row.Length with
                    | x when x = expectedLength -> row
                    | x when x > expectedLength -> row[0..expectedLength-1]
                    | x when x < expectedLength ->
                        let diff = expectedLength - x
                        let padding = Array.init diff (fun _ -> 0.0)
                        padding |> Array.append row
                    | _ -> failwith "should never get here"
            )
        
        let answer = MathNet.Numerics.Statistics.Correlation.PearsonMatrix(matrixToSend)
        
        [|0..answer.RowCount-1|]
        |> Array.mapi (fun index row ->
            match index with
            | x when invalidIndexes.Contains(x) -> Array.init answer.ColumnCount (fun _ -> 0.0)
            | _ ->
                answer.Row(row).ToArray()
                |> Array.mapi (fun i value -> if invalidIndexes.Contains(i) then 0.0 else value)
        )
        
    let dailyPLAndGain (bars:PriceBars) (optionalPosition:StockPositionWithCalculations option) =
        
        let firstBar = 0
        let lastBar = bars.Bars.Length - 1
        
        let numberOfShares, costBasis, firstStop =
            match optionalPosition with
            | Some position ->
                let numberOfSharesMultiplier = if position.IsShort then -1.0m else 1.0m
                position.CompletedPositionShares * numberOfSharesMultiplier, position.AverageCostPerShare, position.FirstStop()
            | None -> 1m, bars.Bars[0].Close, None
        
        
        let zeroLine = ChartAnnotationLine(0.0m, ChartAnnotationLineType.Horizontal) |> Some
        let stopLine =
            match firstStop with
            | None -> None
            | Some stopPrice -> ChartAnnotationLine(stopPrice, ChartAnnotationLineType.Horizontal) |> Some
            
        let close = ChartDataPointContainer<decimal>("Close", DataPointChartType.Line, stopLine)
        let profit = ChartDataPointContainer<decimal>("Profit", DataPointChartType.Line, zeroLine)
        let gainPct = ChartDataPointContainer<decimal>("Gain %", DataPointChartType.Line, zeroLine)
        let obv = ChartDataPointContainer<decimal>("OBV", DataPointChartType.Line, zeroLine)
        
        for i in firstBar..lastBar do
            let bar = bars.Bars[i]
            
            let currentClose = bar.Close
            let currentGainPct = (currentClose - costBasis) / costBasis * 100.0m
            let currentProfit = numberOfShares * (currentClose - costBasis)
            
            let obvValue =
                match obv.Data.Count with
                | 0 -> 0.0m
                | _ ->
                    let lastObvValue = obv.Data[obv.Data.Count - 1].Value
                    let prevClose = bars.Bars[i - 1].Close
                    match currentClose > prevClose with
                    | true -> lastObvValue + decimal bar.Volume
                    | false -> lastObvValue - decimal bar.Volume
            
            close.Add(bar.Date, currentClose)
            profit.Add(bar.Date, currentProfit)
            gainPct.Add(bar.Date, currentGainPct)
            obv.Add(bar.Date, obvValue)
        
        {
            DailyProfit = profit
            DailyGainPct = gainPct
            DailyObv = obv
            DailyClose = close
        }
