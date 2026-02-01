namespace core.fs.Services.Analysis

open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.GapAnalysis


module MultipleBarPriceAnalysis =
    
    // type IMultipleBarPriceAnalysis =
    //     abstract Run : currentPrice:decimal -> prices:PriceBar array -> seq<AnalysisOutcome>
    //     
    module MultipleBarOutcomeKeys =
        let LowestPrice = "LowestPrice"
        let LowestPriceDaysAgo = "LowestPriceDaysAgo"
        let HighestPrice = "HighestPrice"
        let HighestPriceDaysAgo = "HighestPriceDaysAgo"
        let AverageVolume = "AverageVolume"
        let PercentBelowHigh = "PercentBelowHigh"
        let PercentAboveLow = "PercentAboveLow"
        let EMA20AboveSMA50Bars = "EMA20AboveSMA50Bars"
        let SMA50AboveSMA200Bars = "SMA50AboveSMA200Bars"
        let PriceAboveEMA20Bars = "PriceAboveEMA20Bars"
        let CurrentPrice = "CurrentPrice"
        let PercentChangeAverage = "PercentChangeAverage"
        let PercentChangeStandardDeviation = "PercentChangeStandardDeviation"
        let EarliestPrice = "EarliestPrice"
        let Gain = "Gain"
        let AverageTrueRange = "AverageTrueRange"
        let AverageTrueRangePercentage = "AverageTrueRangePercentage"
        let GreenStreak = "GreenStreak"
        let GapPercentage = "GapPercentage"
        let OnBalanceVolume = "OBV"
        let AccumulationDistribution = "A/D"
        
        let private MovingAverage interval exponential =
            let ``type`` = match exponential with | true -> "ema" | false -> "sma"
            $"{``type``}_{interval}"

        let SimpleMovingAverage interval = MovingAverage interval false
        let ExponentialMovingAverage interval = MovingAverage interval true

    module MultipleBarPriceAnalysisConstants =
        let NumberOfDaysForRecentAnalysis = 60
        
    
    module Indicators =
   
        let atrPeriod = 14
            
        type ATRContainer(period, dataPoints) =
            member this.Period = period
            member this.DataPoints = dataPoints
            
        let private averateTrueRangeInternal func (prices:PriceBars) =
            let dataPoints =
                prices.Bars
                |> Array.pairwise
                |> Array.map (fun (a, b) -> b, a |> Some |> func b)
                |> Array.windowed atrPeriod
                |> Array.map (fun x ->
                    let average = x |> Array.averageBy (fun (_, v) -> v)
                    let date = x |> Array.last |> fun (b, _) -> b.Date
                
                    DataPoint<decimal>(date, average)
                )
                
            ATRContainer(atrPeriod, dataPoints)
            
        let averageTrueRage (prices:PriceBars) =
            averateTrueRangeInternal (fun (x:PriceBar) -> x.TrueRange) prices
            
        let averageTrueRangePercentage (prices:PriceBars) =
            averateTrueRangeInternal (fun (x:PriceBar) -> x.TrueRangePercentage) prices
            
        let onBalanceVolume (prices:PriceBar[]) =
            
            [|0..prices.Length - 1|]
            |> Array.mapFold (fun prevObv i ->
                
                let currentBar = prices[i]
                
                let obv =
                    match i with
                    | 0 -> 0m
                    | _ ->
                        let previousBar = prices[i - 1]
                        prevObv + match currentBar.Close with
                                  | close when close > previousBar.Close -> decimal currentBar.Volume
                                  | close when close < previousBar.Close -> -(decimal currentBar.Volume)
                                  | _ -> 0m
                
                let result = DataPoint<decimal>(currentBar.Date, obv)
                result, obv
            ) 0m
            |> fst
            
        let accumulationDistribution (prices:PriceBar[]) =
            
            [|0..prices.Length - 1|]
            |> Array.mapFold (fun prevAd i ->
                
                let currentBar = prices[i]
                
                let moneyFlowMultiplier =
                    let range = currentBar.High - currentBar.Low
                    match range with
                    | 0m -> 0m
                    | _ -> ((currentBar.Close - currentBar.Low) - (currentBar.High - currentBar.Close)) / range
                
                let moneyFlowVolume = moneyFlowMultiplier * (decimal currentBar.Volume)
                
                let ad = prevAd + moneyFlowVolume
                
                let result = DataPoint<decimal>(currentBar.Date, ad)
                result, ad
            ) 0m
            |> fst
                
    module MovingAveragesAnalysis =
            
        let priceStreakDetection (sequenceA:decimal option array) (sequenceB:decimal option array) =
            let sequence =
                Array.zip sequenceA sequenceB
                |> Array.filter (fun (a, b) -> a.IsSome && b.IsSome)
                |> Array.map (fun (a, b) -> a.Value - b.Value)
                |> Array.map (fun v -> v > 0m)
                |> Array.rev
                
            let findIndexOrReturnLength func arr =
                arr
                |> Array.tryFindIndex func
                |> Option.defaultWith (fun () -> arr.Length)
                |> decimal
        
            match sequence with
            | [||] -> None
            | _ ->
                match sequence[0] with
                | true ->
                    OutcomeType.Positive,
                    sequence |> findIndexOrReturnLength (fun v -> v = false)
                | false ->
                    OutcomeType.Negative,
                    sequence |> findIndexOrReturnLength (fun v -> v = true) |> fun x -> x * -1m
                |> Some    
            
        let private generateSMAOutcomes (smaContainer: MovingAveragesContainer) =
            
            smaContainer.All
            |> Array.map (fun ma ->
                
                let value =
                    match ma.LastValue with
                    | Some v -> v
                    | None -> 0m
                    
                let key = match ma.Exponential with
                          | true -> MultipleBarOutcomeKeys.ExponentialMovingAverage(ma.Interval)
                          | false -> MultipleBarOutcomeKeys.SimpleMovingAverage(ma.Interval)
                    
                AnalysisOutcome(
                    key,
                    OutcomeType.Neutral,
                    value,
                    ValueFormat.Currency,
                    $"{key} is {value}"
                )
            )   
            
        let private generatePriceAboveEMA20Outcome (bars:PriceBars) (container:MovingAveragesContainer) =
            
            let closingPrices = bars.ClosingPrices() |> Array.map (fun x -> x |> Some)
            
            let outcomeTypeAndValueOption = priceStreakDetection closingPrices container.ema20.Values
                
            match outcomeTypeAndValueOption with
            | None -> None
            | Some (outcomeType, value) ->
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.PriceAboveEMA20Bars,
                    outcomeType = outcomeType,
                    value = value,
                    valueType = ValueFormat.Number,
                    message = "Price has been " + (if outcomeType = OutcomeType.Negative then "below" else "above") + $" EMA 20 for {abs value} days"
                ) |> Some
            
        let private generateSMA20Above50DaysOutcome (smaContainer: MovingAveragesContainer) =
            
            let outcomeTypeAndValueOption = priceStreakDetection smaContainer.ema20.Values smaContainer.sma50.Values
            match outcomeTypeAndValueOption with
            | None -> None
            | Some (outcomeType, value) ->
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.EMA20AboveSMA50Bars,
                    outcomeType = outcomeType,
                    value = value,
                    valueType = ValueFormat.Number,
                    message = "EMA 20 has been " + (if outcomeType = OutcomeType.Negative then "below" else "above") + $" SMA 50 for {value} bars"
                ) |> Some
                
        let private generateSMA50Above200DaysOutcome (smaContainer: MovingAveragesContainer) =
                
            let outcomeTypeAndvalueOption = priceStreakDetection smaContainer.sma50.Values smaContainer.sma200.Values
            match outcomeTypeAndvalueOption with
            | None -> None
            | Some (outcomeType, value) ->
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.SMA50AboveSMA200Bars,
                    outcomeType = outcomeType,
                    value = value,
                    valueType = ValueFormat.Number,
                    message = "SMA 50 has been " + (if outcomeType = OutcomeType.Negative then "below" else "above") + $" SMA 200 for {value} bars"
                ) |> Some
            
        let generate (prices: PriceBars) =
            
            let smaContainer =  prices |> MovingAveragesContainer.Generate
            
            let streakOutcomes =
                [|
                    generatePriceAboveEMA20Outcome prices
                    generateSMA20Above50DaysOutcome
                    generateSMA50Above200DaysOutcome
                |]
                |> Array.map (fun func -> func smaContainer)
                |> Array.choose id
            
            [
                yield! smaContainer |> generateSMAOutcomes
                yield! streakOutcomes
            ]
       
    module VolumeAnalysis =
        
        let private generateAverageVolumeOutcome (prices: PriceBars) =
            
            let values = MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis |> prices.LatestOrAll |> fun x -> x.Volumes()
                
            let averageVolume = (values |> Array.sum) / (decimal values.Length) |> int64
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.AverageVolume,
                outcomeType = OutcomeType.Neutral,
                value = averageVolume,
                valueType = ValueFormat.Number,
                message = $"Average volume over the last {values.Length} bars is {averageVolume}"
            )
            
        let generate (prices: PriceBars) =
            [prices |> generateAverageVolumeOutcome]

    module PriceAnalysis =
        
        let private recentBars (bars:PriceBars) =
            MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis |> bars.LatestOrAll
                
        let private generateEarliestPriceOutcome (prices: PriceBars) =
            
            let earliestPrice = prices.First
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.EarliestPrice,
                outcomeType = OutcomeType.Neutral,
                value = earliestPrice.Close,
                valueType = ValueFormat.Currency,
                message = $"Earliest price was {earliestPrice.Close} on {earliestPrice.Date}"
            )
            
        let private generateLowestPriceOutcome (prices: PriceBars) =
            
            let lowest = prices.Bars |> Array.minBy (fun p -> p.Close)
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.LowestPrice,
                outcomeType = OutcomeType.Neutral,
                value = lowest.Close,
                valueType = ValueFormat.Currency,
                message = $"Lowest price was {lowest.Close} on {lowest.Date}"
            )
            
        let private generateLowestPriceDaysAgoOutcome (prices: PriceBars) =
            
            let lowest = prices.Bars |> Array.minBy (fun p -> p.Close)
            let lowestPriceDaysAgo = System.Math.Floor(System.DateTimeOffset.UtcNow.Subtract(lowest.Date).TotalDays)
            let lowestPriceDaysAgoOutcomeType = if lowestPriceDaysAgo <= 30 then OutcomeType.Negative else OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.LowestPriceDaysAgo,
                outcomeType = lowestPriceDaysAgoOutcomeType,
                value = (lowestPriceDaysAgo |> decimal),
                valueType = ValueFormat.Number,
                message = $"Lowest price was {lowest.Close} on {lowest.Date} which was {lowestPriceDaysAgo} days ago"
            )
            
        let private generatePercentAboveLowOutcome (prices: PriceBars) =
            
            let lowest = prices.Bars |> Array.minBy (fun p -> p.Close)
            let percentAboveLow =
                match lowest.Close with
                | 0m -> 0m
                | _ -> (prices.Last.Close - lowest.Close) / lowest.Close
            let percentAboveLowOutcomeType = OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentAboveLow,
                outcomeType = percentAboveLowOutcomeType,
                value = percentAboveLow,
                valueType = ValueFormat.Percentage,
                message = $"Percent above recent low: {percentAboveLow}%%"
            )
            
        let private generateHighestPriceOutcome (prices: PriceBars) =
            
            let highest = prices.Bars |> Array.maxBy (fun p -> p.Close)
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.HighestPrice,
                outcomeType = OutcomeType.Neutral,
                value = highest.Close,
                valueType = ValueFormat.Currency,
                message = $"Highest price was {highest.Close} on {highest.Date}"
            )
            
        let private generateHighestPriceDaysAgoOutcome (prices: PriceBars) =
            
            let highest = prices.Bars |> Array.maxBy (fun p -> p.Close)
            let highestPriceDaysAgo = System.Math.Round(System.DateTimeOffset.UtcNow.Subtract(highest.Date).TotalDays, 0)
            let highestPriceDaysAgoOutcomeType = if highestPriceDaysAgo <= 30 then OutcomeType.Positive else OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.HighestPriceDaysAgo,
                outcomeType = highestPriceDaysAgoOutcomeType,
                value = (highestPriceDaysAgo |> decimal),
                valueType = ValueFormat.Number,
                message = $"Highest price was {highest.Close} on {highest.Date} which was {highestPriceDaysAgo} days ago"
            )
            
        let private generatePercentBelowHighOutcome (prices: PriceBars) =
            
            let highest = prices.Bars |> Array.maxBy (fun p -> p.Close)
            let percentBelowHigh = (highest.Close - prices.Last.Close) / highest.Close
            let percentBelowHighOutcomeType = OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentBelowHigh,
                outcomeType = percentBelowHighOutcomeType,
                value = percentBelowHigh,
                valueType = ValueFormat.Percentage,
                message = $"Percent below recent high: {percentBelowHigh}%%"
            )
            
        let private generateGainOutcome (prices: PriceBars) =
            
            let gain =
                match prices.First.Close with
                | 0m -> 0m
                | _ -> (prices.Last.Close - prices.First.Close) / prices.First.Close
                
            let gainOutcomeType = if gain > 0m then OutcomeType.Positive else OutcomeType.Negative
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.Gain,
                outcomeType = gainOutcomeType,
                value = gain,
                valueType = ValueFormat.Percentage,
                message = $"Gain from earliest to latest: {gain}%%"
            )
            
        let private generateGapOutcome (prices:PriceBars) =
            let gaps = prices |> detectGaps Constants.NumberOfDaysForRecentAnalysis
            let gap = gaps |> Array.tryFind (fun x -> x.Bar = prices.Last)
                    
            let outcomeType =
                match gap with
                | Some x when x.Type = GapType.Up -> OutcomeType.Positive
                | Some x when x.Type = GapType.Down -> OutcomeType.Negative
                | _ -> OutcomeType.Neutral
                
            let gapValue =
                match gap with
                | Some x -> x.GapSizePct
                | _ -> 0m
                
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.GapPercentage,
                outcomeType = outcomeType,
                value = gapValue,
                valueType = ValueFormat.Percentage,
                message =
                    match gap with
                    | Some x -> $"Gap of {x:P2}"
                    | _ -> "No gap detected"
            )
            
        let private generatePercentChangeAverageOutcome (prices: PriceBars) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars false
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeAverage,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.mean,
                valueType = ValueFormat.Percentage,
                message = $"%% Change Average: {descriptor.mean}"
            )
            
        let private generatePercentChangeStandardDeviationOutcome (prices: PriceBars) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars false
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeStandardDeviation,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.stdDev,
                valueType = ValueFormat.Percentage,
                message = $"%% Change StD: {descriptor.stdDev}"
            )
            
        let generateAverageTrueRangeOutcome (prices:PriceBars) =
            
            let dataPoints = prices |> Indicators.averageTrueRage |> _.DataPoints
            
            match dataPoints with
            | [||] -> None
            | _ ->
                let value = dataPoints |> Array.last |> _.Value
        
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.AverageTrueRange,
                    outcomeType = OutcomeType.Neutral,
                    value = value,
                    valueType = ValueFormat.Currency,
                    message = $"Average True Range: {value}"
                ) |> Some
                
        let generateAverageTrueRangePercentageOutcome (prices:PriceBars) =
        
            let dataPoints = prices |> Indicators.averageTrueRangePercentage |> _.DataPoints
            
            match dataPoints with
            | [||] -> None
            | _ ->
                let value = dataPoints |> Array.last |> _.Value
                
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.AverageTrueRangePercentage,
                    outcomeType = OutcomeType.Neutral,
                    value = value,
                    valueType = ValueFormat.Percentage,
                    message = $"Average True Range Percentage: {value}"
                ) |> Some
                
        let generateGreenStreakOutcome (prices:PriceBars) =
            
            let greenStreak =
                prices.Bars
                |> Array.map (fun p -> p.Close)
                |> Array.pairwise
                |> Array.map (fun (a, b) -> b - a)
                |> Array.map (fun v -> v > 0m)
                |> Array.rev
                |> Array.tryFindIndex (fun v -> v = false)
                |> Option.defaultWith (fun () -> prices.Bars.Length)
                |> decimal
                
            let outcomeType =
                match greenStreak with
                | x when x < 5m -> OutcomeType.Neutral
                | _ -> OutcomeType.Positive
                
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.GreenStreak,
                outcomeType = outcomeType,
                value = greenStreak,
                valueType = ValueFormat.Number,
                message = $"Green Streak: {greenStreak}"
            )
            
        let private generateCurrentPriceOutcome (prices:PriceBars) =
            
            let currentPrice = prices.Last.Close
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.CurrentPrice,
                outcomeType = OutcomeType.Neutral,
                value = currentPrice,
                valueType = ValueFormat.Currency,
                message = $"Current price is {currentPrice:C2}"
            )
            
        let private generateOnBalanceVolumeOutcome (prices:PriceBars) =
            
            let obv = Indicators.onBalanceVolume prices.Bars |> Array.last |> _.Value
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.OnBalanceVolume,
                outcomeType = OutcomeType.Neutral,
                value = obv,
                valueType = ValueFormat.Currency,
                message = $"On Balance Volume: {obv}"
            )
            
        let private generateAccumulationDistributionOutcome (prices:PriceBars) =
            
            let ad = Indicators.accumulationDistribution prices.Bars |> Array.last |> _.Value
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.AccumulationDistribution,
                outcomeType = OutcomeType.Neutral,
                value = ad,
                valueType = ValueFormat.Number,
                message = $"Accumulation/Distribution: {ad:N0}"
            )
            
        let generate (prices: PriceBars) =
            
            let atrOutcome = prices |> generateAverageTrueRangeOutcome
            let atrOutcomePercent = prices |> generateAverageTrueRangePercentageOutcome
                    
            [
                prices |> generateCurrentPriceOutcome
                prices |> generateEarliestPriceOutcome
                prices |> generateLowestPriceOutcome
                prices |> generateLowestPriceDaysAgoOutcome
                prices |> generatePercentAboveLowOutcome
                prices |> generateHighestPriceOutcome
                prices |> generateHighestPriceDaysAgoOutcome
                prices |> generatePercentBelowHighOutcome
                prices |> generateGainOutcome
                prices |> generatePercentChangeAverageOutcome
                prices |> generatePercentChangeStandardDeviationOutcome
                if atrOutcome.IsSome then atrOutcome.Value
                if atrOutcomePercent.IsSome then atrOutcomePercent.Value
                prices |> generateGapOutcome
                prices |> generateGreenStreakOutcome
                prices |> generateOnBalanceVolumeOutcome
                prices |> generateAccumulationDistributionOutcome
            ]
    
    let run prices =
        [
            yield! prices |> PriceAnalysis.generate
            yield! prices |> VolumeAnalysis.generate
            yield! prices |> MovingAveragesAnalysis.generate    
        ]
            
    let evaluate _ = []
