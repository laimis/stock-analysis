namespace core.fs.Services.Analysis

open System
open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services
open core.fs.Services.MultipleBarPriceAnalysis

module SingleBarPriceAnalysis =
    
    module SingleBarAnalysisConstants =
        let NumberOfDaysForRecentAnalysis = 60
    
    module SingleBarOutcomeKeys =
        let PriceAbove20SMA = "PriceAbove20SMA"
        let PriceAbove20SMADays = "PriceAbove20SMADays"
        let PriceBelow20SMA = "PriceBelow20SMA"
        let PriceBelow20SMADays = "PriceBelow20SMADays"
        let RelativeVolume = "RelativeVolume"
        let Volume = "Volume"
        let PercentChange = "PercentChange"
        let ClosingRange = "ClosingRange"
        let Open = "Open"
        let Close = "Close"
        let SMA20Above50Days = "SMA20Above50Days"
        let GapPercentage = "GapPercentage"
        let NewHigh = "NewHigh"
        let NewLow = "NewLow"
        let SigmaRatio = "SigmaRatio"
        let TrueRange = "TrueRange"
    
    let smaAnalysis (bars:PriceBars) =
        
        let outcomes =  bars |> SMAAnalysis.generate
        
        let sma20Above50Outcome = outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarOutcomeKeys.SMA20Above50Days)
        let sma20outcome = outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarOutcomeKeys.SMA(20))
        let priceAbove20SMAOutcome = outcomes |> Seq.tryFind (fun x -> x.Key = MultipleBarOutcomeKeys.PriceAbove20SMADays)
        
        [
            if sma20Above50Outcome.IsSome then
                AnalysisOutcome (SingleBarOutcomeKeys.SMA20Above50Days, sma20Above50Outcome.Value.OutcomeType, sma20Above50Outcome.Value.Value, sma20Above50Outcome.Value.ValueType, sma20Above50Outcome.Value.Message)
                
            if sma20outcome.IsSome && sma20outcome.Value.Value <> 0m then
                let currentBar = bars.Last        
                let pctDiff = (currentBar.Close - sma20outcome.Value.Value) / sma20outcome.Value.Value
                
                AnalysisOutcome (SingleBarOutcomeKeys.PriceAbove20SMA, (if pctDiff >= 0m then OutcomeType.Positive else OutcomeType.Negative), pctDiff, ValueFormat.Percentage, "Percentage that price is above 20 day SMA")
                
            if priceAbove20SMAOutcome.IsSome then
                AnalysisOutcome (SingleBarOutcomeKeys.PriceAbove20SMADays, priceAbove20SMAOutcome.Value.OutcomeType, priceAbove20SMAOutcome.Value.Value, priceAbove20SMAOutcome.Value.ValueType, priceAbove20SMAOutcome.Value.Message)
        ]
        
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
                
                // generate percent change statistical data for NumberOfDaysForRecentAnalysis days
                let descriptor = SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis |> previousBars.LatestOrAll |> PercentChangeAnalysis.calculateForPriceBars
                
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
                        
                let sigmaRatioOutcomeType =
                    match sigmaRatio with
                    | x when x > 1m -> OutcomeType.Positive
                    | x when x < -1m -> OutcomeType.Negative
                    | _ -> OutcomeType.Neutral
            
                let sigmaRatio = Math.Round (sigmaRatio, 2)
                
                yield AnalysisOutcome (SingleBarOutcomeKeys.SigmaRatio,sigmaRatioOutcomeType, sigmaRatio, ValueFormat.Number, $"Sigma ratio is {sigmaRatio}.")
                
                
            // see if there was a gap down or gap up
            let gaps = GapAnalysis.detectGaps bars SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis
            
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
            
            if previousBars.Length > 0 then
                let trueRange = previousBars.Last |> Some |> currentBar.TrueRange
                yield AnalysisOutcome (SingleBarOutcomeKeys.TrueRange, OutcomeType.Neutral, trueRange, ValueFormat.Currency, "True range")
                
            yield MultipleBarPriceAnalysis.PriceAnalysis.generateGreenStreakOutcome bars
        ]
        
    let volumeAnalysis (bars:PriceBars) =
        
        let volumeStats = SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis |> bars.LatestOrAll |> fun x -> x.Volumes() |> DistributionStatistics.calculate
        
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
            yield! bars |> smaAnalysis
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
                "SMA 20 below SMA 50",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.SMA20Above50Days,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.SMA20Above50Days && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price above 20 SMA",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.PriceAbove20SMA,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceAbove20SMA && o.Value >= 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price below 20 SMA",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.PriceBelow20SMA,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceBelow20SMA && o.Value < 0m) ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price went above 20 SMA",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.PriceAbove20SMADays,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceAbove20SMADays && o.Value = 1m)  ]
            )
            
            AnalysisOutcomeEvaluation(
                "Price went below 20 SMA",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.PriceBelow20SMADays,
                tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = SingleBarOutcomeKeys.PriceBelow20SMADays && o.Value = -1m)  ]
            )
        ]