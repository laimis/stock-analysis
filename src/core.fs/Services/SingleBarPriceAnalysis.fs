namespace core.fs.Services.Analysis

open System
open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services

module SingleBarPriceAnalysis =
    
    module SingleBarOutcomeKeys =
        let PriceAboveEMA20Bars = "PriceAboveEMA20Bars"
        let PriceAboveSMA200 = "PriceAboveSMA200"
        let PriceAboveSMA200Bars = "PriceAboveSMA200Bars"
        let PriceBelowEMA20Bars = "PriceBelowEMA20Bars"
        let RelativeVolume = "RelativeVolume"
        let Volume = "Volume"
        let PercentChange = "PercentChange"
        let DollarChange = "DollarChange"
        let ClosingRange = "ClosingRange"
        let Open = "Open"
        let Close = "Close"
        let EMA20AboveSMA50Bars = "EMA20AboveSMA50Bars"
        let SMA50AboveSMA200Bars = "SMA50AboveSMA200Bars"
        let GapPercentage = "GapPercentage"
        let NewHigh = "NewHigh"
        let NewLow = "NewLow"
        let SigmaRatio = "SigmaRatio"
        let TrueRange = "TrueRange"
        let TrueRangePercentage = "TrueRangePercentage"
        let DollarChangeVsATR = "DollarChangeVsATR"
    
    let movingAveragesAnalysis (bars:PriceBars) =
        
        // only do this analysis for daily bars
        match bars.Frequency with
        | Daily ->
            let outcomes =  bars |> MultipleBarPriceAnalysis.MovingAveragesAnalysis.generate
            
            let ema20AboveSMA50Outcome =
                outcomes
                |> Seq.tryFind (fun x -> x.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EMA20AboveSMA50Bars)
                |> Option.map (fun o -> AnalysisOutcome (SingleBarOutcomeKeys.EMA20AboveSMA50Bars, o.OutcomeType, o.Value, o.ValueType, o.Message))
                
            let sma50AboveSMA200Outcome =
                outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA50AboveSMA200Bars)
                |> Option.map (fun o -> AnalysisOutcome (SingleBarOutcomeKeys.SMA50AboveSMA200Bars, o.OutcomeType, o.Value, o.ValueType, o.Message))
                
            let sma200outcome =
                outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage 200)
                |> Option.filter (fun o -> o.Value = 0m |> not)
                |> Option.map (fun o ->
                    let currentBar = bars.Last        
                    let pctDiff = (currentBar.Close - o.Value) / o.Value
                    AnalysisOutcome (SingleBarOutcomeKeys.PriceAboveSMA200, (if pctDiff >= 0m then OutcomeType.Positive else OutcomeType.Negative), pctDiff, ValueFormat.Percentage, "Percentage that price is above 200 day SMA")   
                )
                
            let priceAboveEMA20BarsOutcome =
                outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAboveEMA20Bars)
                |> Option.map (fun o -> AnalysisOutcome (SingleBarOutcomeKeys.PriceAboveEMA20Bars, o.OutcomeType, o.Value, o.ValueType, o.Message))
            
            [
                ema20AboveSMA50Outcome
                sma50AboveSMA200Outcome
                sma200outcome
                priceAboveEMA20BarsOutcome
            ] |> List.choose id
        | _ -> []
        
    let priceAnalysis (bars:PriceBars) =
        
        let currentBar = bars.Last
        let previousBars = bars.AllButLast()
        let range = currentBar.ClosingRange()
        
        [
            yield AnalysisOutcome (SingleBarOutcomeKeys.Open, OutcomeType.Neutral, currentBar.Open, ValueFormat.Currency, "Open price")
            yield AnalysisOutcome (SingleBarOutcomeKeys.Close, OutcomeType.Neutral, currentBar.Close, ValueFormat.Currency, "Close price")
            yield AnalysisOutcome (SingleBarOutcomeKeys.ClosingRange, (if range >= 0.80m then OutcomeType.Positive else OutcomeType.Neutral), range, ValueFormat.Percentage, $"Closing range is {range}.")
            
            if previousBars.Length > 0 then            
                // use yesterday's close as reference
                let yesterday = previousBars.Last
                // today's change from yesterday's close
                let percentChange = (currentBar.Close - yesterday.Close) / yesterday.Close
                let percentChangeOutcomeType =
                    match percentChange with
                    | x when x >= 0m -> OutcomeType.Positive
                    | _ -> OutcomeType.Negative
            
                // add change as outcome
                yield AnalysisOutcome (SingleBarOutcomeKeys.PercentChange, percentChangeOutcomeType, percentChange, ValueFormat.Percentage, $"%% change from close is {percentChange}.")
            
                let dollarChange = currentBar.Close - yesterday.Close    
                
                // add dollar change as outcome
                yield AnalysisOutcome (SingleBarOutcomeKeys.DollarChange, percentChangeOutcomeType, dollarChange, ValueFormat.Currency, $"Dollar change from close is {currentBar.Close - yesterday.Close}.")
                
                // generate percent change statistical data for NumberOfDaysForRecentAnalysis days
                let descriptor = Constants.NumberOfDaysForRecentAnalysis |> previousBars.LatestOrAll |> PercentChangeAnalysis.calculateForPriceBars true
                
                // for some price feeds, price has finished changing, so mean and
                // standard deviation will be 0, we need to check for that so that we don't divide by 0
                let sigmaRatioDenominator = 
                    match percentChange with
                    | x when x >= 0m -> descriptor.mean + descriptor.stdDev
                    | _ -> descriptor.mean - descriptor.stdDev
                
                let sigmaRatio = 
                    match sigmaRatioDenominator with
                    | 0m -> 0m
                    | _ -> Math.Abs(percentChange * 100m/sigmaRatioDenominator)
                        
                let sigmaRatio = Math.Round (sigmaRatio, 2)
                
                yield AnalysisOutcome (SingleBarOutcomeKeys.SigmaRatio, percentChangeOutcomeType, sigmaRatio, ValueFormat.Number, $"Sigma ratio is {sigmaRatio}.")
                
                let trueRange = previousBars.Last |> Some |> currentBar.TrueRange
                
                yield AnalysisOutcome (SingleBarOutcomeKeys.TrueRange, OutcomeType.Neutral, trueRange, ValueFormat.Currency, "True range")
                
                let trueRangePercentage = previousBars.Last |> Some |> currentBar.TrueRangePercentage
                
                yield AnalysisOutcome (SingleBarOutcomeKeys.TrueRangePercentage, OutcomeType.Neutral, trueRangePercentage, ValueFormat.Percentage, "True range percentage")
                
                let atr = MultipleBarPriceAnalysis.PriceAnalysis.generateAverageTrueRangeOutcome bars |> Option.get
                
                yield AnalysisOutcome (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageTrueRange, atr.OutcomeType, atr.Value, atr.ValueType, atr.Message)
                
                let atrp = MultipleBarPriceAnalysis.PriceAnalysis.generateAverageTrueRangePercentageOutcome bars |> Option.get
                
                yield AnalysisOutcome (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageTrueRangePercentage, atrp.OutcomeType, atrp.Value, atrp.ValueType, atrp.Message)
                
                let changeRatioVsATR = Math.Round (dollarChange / atr.Value, 2)
                
                yield AnalysisOutcome (SingleBarOutcomeKeys.DollarChangeVsATR, percentChangeOutcomeType, changeRatioVsATR, ValueFormat.Number, $"Dollar change vs ATR is {changeRatioVsATR}.")
                
            // see if there was a gap down or gap up
            let gaps = bars |> GapAnalysis.detectGaps Constants.NumberOfDaysForRecentAnalysis
            
            let gap = gaps |> Seq.tryFind _.Bar.Equals(currentBar)
            
            let gapType = 
                match gap with
                | Some x when x.GapSizePct > 0m -> OutcomeType.Positive
                | Some x when x.GapSizePct < 0m -> OutcomeType.Negative
                | _ -> OutcomeType.Neutral
                
            let gapSizePct = 
                match gap with
                | Some x -> x.GapSizePct
                | _ -> 0m
                
            yield AnalysisOutcome (SingleBarOutcomeKeys.GapPercentage, gapType, gapSizePct, ValueFormat.Percentage, $"Gap is {gapSizePct}%%.")
            
            // see if the latest bar is a one year high or low
            let oneYearAgoDate = currentBar.Date.AddYears(-1)
            let oneYearAgoIndex =
                previousBars.Bars
                |> Array.mapi (fun i x -> (x, i))
                |> Array.filter (fun x -> x |> fst |> fun x -> x.Date <= oneYearAgoDate)
                |> Array.map (fun x -> x |> snd)
                |> Array.tryLast
                
            // now create a new high sequence from that bar
            let oneYearAgoBars = 
                match oneYearAgoIndex with 
                | Some x -> bars.Bars[x..]
                | _ -> bars.Bars  // if we don't have one year old bars, then just use all bars
                
            let initValue = [|oneYearAgoBars[0]|]
            
            let newHighsSequence =
                oneYearAgoBars
                |> Array.fold (fun (acc:PriceBar array) x ->
                    let last = acc[acc.Length - 1]
                    if x.High > last.High then
                        Array.append acc [|x|]
                    else
                        acc
                ) initValue
                
            // new high will get triggered only if it's at least 2 months ago
            let newHighSequenceLastBar = newHighsSequence[newHighsSequence.Length - 1]
             
            let dayDifferenceBetweenBars =
                if newHighsSequence.Length >= 2 then
                    let newHighSequenceTwoBarsAgo = newHighsSequence[newHighsSequence.Length - 2]
                    (newHighSequenceLastBar.Date - newHighSequenceTwoBarsAgo.Date).TotalDays
                else
                0.0
             
            let newHigh = newHighSequenceLastBar.Equals(currentBar) && dayDifferenceBetweenBars > 60
             
            let newLow = 
                 previousBars.Bars
                 |> Array.filter (fun x -> x.Date >= oneYearAgoDate)
                 |> Array.forall (fun x -> x.Low > currentBar.Low)
                 
             // add new high as outcome
            yield AnalysisOutcome (SingleBarOutcomeKeys.NewHigh, (if newHigh then OutcomeType.Positive else OutcomeType.Neutral), (if newHigh then 1m else 0m), ValueFormat.Boolean, "New high reached")
             
             // add new low as outcome
            yield AnalysisOutcome (SingleBarOutcomeKeys.NewLow, (if newLow then OutcomeType.Negative else OutcomeType.Neutral), (if newLow then -1m else 0m), ValueFormat.Boolean, "New low reached")
            
            yield MultipleBarPriceAnalysis.PriceAnalysis.generateGreenStreakOutcome bars
        ]
        
    let volumeAnalysis (bars:PriceBars) =
        
        let volumeStats = Constants.NumberOfDaysForRecentAnalysis |> bars.LatestOrAll |> fun x -> x.Volumes() |> DistributionStatistics.calculate
        
        let currentBar = bars.Last
        
        let relativeVolume = 
            match volumeStats.mean with
            | 0m -> 0m
            | _ -> Math.Round(decimal(currentBar.Volume) / volumeStats.mean, 2)
            
        let priceDirection =
            match currentBar.Close > currentBar.Open with
            | true -> OutcomeType.Positive
            | _ -> OutcomeType.Negative
            
        let relativeVolumeOutcomeType =
            match relativeVolume with
            | x when x >= 0.9m -> priceDirection
            | _ -> OutcomeType.Neutral
        
        let relativeVolumeOutcomeDescription = $"Relative volume is {relativeVolume}x average volume over the last {volumeStats.count} days."
        
        [
            AnalysisOutcome (SingleBarOutcomeKeys.Volume, OutcomeType.Neutral, currentBar.Volume, ValueFormat.Number, "Volume")
            AnalysisOutcome (SingleBarOutcomeKeys.RelativeVolume, relativeVolumeOutcomeType, relativeVolume, ValueFormat.Number, relativeVolumeOutcomeDescription)
        ]
        
    let run bars =
        
        [
            yield! bars |> volumeAnalysis
            yield! bars |> priceAnalysis
            yield! bars |> movingAveragesAnalysis
        ]
        
open SingleBarPriceAnalysis

module SingleBarPriceAnalysisEvaluation =

    let private RelativeVolumeThresholdPositive = 0.9m
    let private SigmaRatioThreshold = 1m
    let private SmallSigmaThreshold = 0.4m
    let private ExcellentClosingRange = 0.80m
    let private LowClosingRange = 0.20m
        
    let evaluate (tickerOutcomes:seq<TickerOutcomes>) =
        
        let highVolumeWithExcellentClosingRangeAndHighPercentage =
            tickerOutcomes
            |> TickerOutcomes.filter [
                (fun o -> o.Key = SingleBarOutcomeKeys.RelativeVolume && o.Value >= RelativeVolumeThresholdPositive)
                (fun o -> o.Key = SingleBarOutcomeKeys.ClosingRange && o.Value >= ExcellentClosingRange)
                (fun o -> o.Key = SingleBarOutcomeKeys.SigmaRatio && o.Value >= SigmaRatioThreshold)
                (fun o -> o.Key = SingleBarOutcomeKeys.PercentChange && o.Value >= 0m) // make sure it's positive
            ]
            
        [
            AnalysisOutcomeEvaluation (
                "High Volume with Excellent Closing Range and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.RelativeVolume,
                highVolumeWithExcellentClosingRangeAndHighPercentage
            )
            
            AnalysisOutcomeEvaluation(
                "Gap ups",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.GapPercentage,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.GapPercentage && o.Value > 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "High Volume and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes |> TickerOutcomes.filter
                    [
                        (fun o -> o.Key = SingleBarOutcomeKeys.RelativeVolume && o.Value >= RelativeVolumeThresholdPositive)
                        (fun o -> o.Key = SingleBarOutcomeKeys.SigmaRatio && o.Value >= SigmaRatioThreshold)
                        (fun o -> o.Key = SingleBarOutcomeKeys.PercentChange && o.Value >= 0m) // make sure it's positive
                    ]
            )
            
            AnalysisOutcomeEvaluation(
                "Excellent Closing Range and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.ClosingRange,
                tickerOutcomes |> TickerOutcomes.filter
                    [
                        (fun o -> o.Key = SingleBarOutcomeKeys.ClosingRange && o.Value >= ExcellentClosingRange)
                        (fun o -> o.Key = SingleBarOutcomeKeys.SigmaRatio && o.Value >= SigmaRatioThreshold)
                        (fun o -> o.Key = SingleBarOutcomeKeys.PercentChange && o.Value >= 0m) // make sure it's positive
                    ]
            )
            
            AnalysisOutcomeEvaluation(
                "New Highs",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.NewHigh,
                tickerOutcomes |> TickerOutcomes.filter [(fun o -> o.Key = SingleBarOutcomeKeys.NewHigh && o.Value > 0m)]
            )
            
            AnalysisOutcomeEvaluation(
                "Low Closing Range",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.ClosingRange,
                tickerOutcomes |> TickerOutcomes.filter [(fun o -> o.Key = SingleBarOutcomeKeys.ClosingRange && o.Value <= LowClosingRange)]
            )
            
            AnalysisOutcomeEvaluation(
                "High Volume with Low Closing Range and Small Percent Change",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes |> TickerOutcomes.filter [
                    (fun o -> o.Key = SingleBarOutcomeKeys.RelativeVolume && o.Value >= RelativeVolumeThresholdPositive)
                    (fun o -> o.Key = SingleBarOutcomeKeys.ClosingRange && o.Value <= LowClosingRange)
                    (fun o -> o.Key = SingleBarOutcomeKeys.SigmaRatio && Math.Abs(o.Value) < SigmaRatioThreshold)
                    (fun o -> o.Key = SingleBarOutcomeKeys.PercentChange && o.Value > 0m)
                ]
            )
            
            AnalysisOutcomeEvaluation(
                "Negative gap downs",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.GapPercentage,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.GapPercentage && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "New Lows",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.NewLow,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.NewLow && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "EMA20 < SMA50 (short-term down)",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.EMA20AboveSMA50Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.EMA20AboveSMA50Bars && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "EMA20 > SMA50 (short-term up)",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.EMA20AboveSMA50Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.EMA20AboveSMA50Bars && o.Value > 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "SMA50 < SMA200 (long-term down)",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.SMA50AboveSMA200Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.SMA50AboveSMA200Bars && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "SMA50 > SMA200 (long-term up)",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.SMA50AboveSMA200Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.SMA50AboveSMA200Bars && o.Value > 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price went above EMA20",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.PriceAboveEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceAboveEMA20Bars && o.Value = 1m)  ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price went below EMA20",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.PriceBelowEMA20Bars,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceAboveEMA20Bars && o.Value = -1m)  ]
            )
            
            AnalysisOutcomeEvaluation(
                "Dollar Change 3x+ ATR Positive",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.DollarChangeVsATR,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.DollarChangeVsATR && o.Value >= 3m)  ]
            )
            
            AnalysisOutcomeEvaluation(
                "Dollar Change 3x+ ATR Negative",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.DollarChangeVsATR,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.DollarChangeVsATR && o.Value <= -3m)  ]
            )
        ]
