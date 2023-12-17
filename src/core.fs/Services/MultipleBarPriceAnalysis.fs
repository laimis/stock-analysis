namespace core.fs.Services

open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis


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
        let SMA20Above50Days = "SMA20Above50Days"
        let PriceAbove20SMADays = "PriceAbove20SMADays"
        let CurrentPrice = "CurrentPrice"
        let PercentChangeAverage = "PercentChangeAverage"
        let PercentChangeStandardDeviation = "PercentChangeStandardDeviation"
        let EarliestPrice = "EarliestPrice"
        let Gain = "Gain"
        let AverageTrueRange = "AverageTrueRange"
        let GreenStreak = "GreenStreak"
        
        let SMA interval = $"sma_%i{interval}"

    module MultipleBarPriceAnalysisConstants =
        let NumberOfDaysForRecentAnalysis = 60
        
    
    module Indicators =
   
        type ATRContainer(period, dataPoints) =
            member this.Period = period
            member this.DataPoints = dataPoints
            
        let averageTrueRage (prices:PriceBars) =
            
            let period = 14
            
            let dataPoints =
                prices.Bars
                |> Array.pairwise
                |> Array.map (fun (a, b) -> b, a |> Some |> b.TrueRange)
                |> Array.windowed period
                |> Array.map (fun x ->
                    let average = x |> Array.averageBy (fun (_, v) -> v)
                    let date = x |> Array.last |> fun (b, _) -> b.Date
                
                    DataPoint<decimal>(date, average)
                )
                
            ATRContainer(period, dataPoints)
                
    module SMAAnalysis =
        
        let private generateSMAOutcomes (smaContainer: SMAContainer) =
            
            smaContainer.All
            |> Array.map (fun sma ->
                
                let value =
                    match sma.LastValue with
                    | Some v -> v
                    | None -> 0m
                    
                AnalysisOutcome(
                    MultipleBarOutcomeKeys.SMA(sma.Interval),
                    OutcomeType.Neutral,
                    value,
                    ValueFormat.Currency,
                    $"SMA {sma.Interval} is {value}"
                )
            )   
            
        let private generatePriceAboveSMA20Outcome (bars:PriceBars) (container:SMAContainer) =
            
            let closingPrices = bars.ClosingPrices()
            
            let priceAndSMA20 =
                container.sma20.Values
                |> Array.zip closingPrices
                |> Array.filter (fun (_, sma20) -> sma20.IsSome)
                |> Array.map (fun (price, sma20) -> (price, sma20.Value))
                |> Array.map (fun (price, sma20) -> (price - sma20) > 0m)
                |> Array.rev

            let findIndexOrReturnLength func arr =
                arr
                |> Array.tryFindIndex func
                |> Option.defaultWith (fun () -> arr.Length)
                |> decimal
                
            match priceAndSMA20 with
            | [||] -> None
            | _ ->
                let outcomeType, value =
                    match priceAndSMA20[0] with
                    | true ->
                        OutcomeType.Positive,
                        priceAndSMA20 |> findIndexOrReturnLength (fun v -> v = false)
                    | false ->
                        OutcomeType.Negative,
                        priceAndSMA20 |> findIndexOrReturnLength (fun v -> v = true) |> fun x -> x * -1m
                        
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.PriceAbove20SMADays,
                    outcomeType = outcomeType,
                    value = value,
                    valueType = ValueFormat.Number,
                    message = "Price has been " + (if outcomeType = OutcomeType.Negative then "below" else "above") + $" SMA 20 for {abs value} days"
                ) |> Some
            
        let private generateSMA20Above50DaysOutcome (smaContainer: SMAContainer) =
            
            let sma20Above50Sequence = 
                smaContainer.sma50.Values
                |> Array.zip smaContainer.sma20.Values 
                |> Array.filter (fun (sma20, sma50) -> sma20.IsSome && sma50.IsSome)
                |> Array.map (fun (sma20, sma50) -> (sma20.Value, sma50.Value))
                |> Array.map (fun (sma20, sma50) -> (sma20 - sma50) > 0m)
                |> Array.rev
                
            let findIndexOrReturnLength func arr =
                arr
                |> Array.tryFindIndex func
                |> Option.defaultWith (fun () -> arr.Length)
                |> decimal
            
            match sma20Above50Sequence with
            | [||] -> None
            | _ ->
                let outcomeType, value =
                    match sma20Above50Sequence[0] with
                    | true ->
                        OutcomeType.Positive,
                        sma20Above50Sequence |> findIndexOrReturnLength (fun v -> v = false)
                    | false ->
                        OutcomeType.Negative,
                        sma20Above50Sequence |> findIndexOrReturnLength (fun v -> v = true)
                
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.SMA20Above50Days,
                    outcomeType = outcomeType,
                    value = value,
                    valueType = ValueFormat.Number,
                    message = "SMA 20 has been " + (if outcomeType = OutcomeType.Negative then "below" else "above") + $" SMA 50 for {value} days"
                ) |> Some
            
        let generate (prices: PriceBars) =
            
            let smaContainer =  prices |> SMAContainer.Generate
            
            let sma20Over50Outcome = smaContainer |> generateSMA20Above50DaysOutcome
            let priceOver20Outcome = smaContainer |> generatePriceAboveSMA20Outcome prices
            
            [
                yield! smaContainer |> generateSMAOutcomes
                if sma20Over50Outcome.IsSome then sma20Over50Outcome.Value
                if priceOver20Outcome.IsSome then priceOver20Outcome.Value
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
            
        let private generatePercentChangeAverageOutcome (prices: PriceBars) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeAverage,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.mean,
                valueType = ValueFormat.Percentage,
                message = $"%% Change Average: {descriptor.mean}"
            )
            
        let private generatePercentChangeStandardDeviationOutcome (prices: PriceBars) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeStandardDeviation,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.stdDev,
                valueType = ValueFormat.Number,
                message = $"%% Change StD: {descriptor.stdDev}"
            )
            
        let private generateAverageTrueRangeOutcome (prices:PriceBars) =
            
            let dataPoints = prices |> Indicators.averageTrueRage |> fun x -> x.DataPoints
            
            match dataPoints with
            | [||] -> None
            | _ ->
                let value = dataPoints |> Array.last |> fun x -> x.Value
        
                AnalysisOutcome(
                    key = MultipleBarOutcomeKeys.AverageTrueRange,
                    outcomeType = OutcomeType.Neutral,
                    value = value,
                    valueType = ValueFormat.Currency,
                    message = $"Average True Range: {value}"
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
            
        let generate (prices: PriceBars) =
            
            let atrOutcome = prices |> generateAverageTrueRangeOutcome
            
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
                prices |> generateGreenStreakOutcome
            ]
    
    let run prices =
        [
            yield! prices |> PriceAnalysis.generate
            yield! prices |> VolumeAnalysis.generate
            yield! prices |> SMAAnalysis.generate    
        ]
            
    module MultipleBarAnalysisOutcomeEvaluation =
        let evaluate _ = []