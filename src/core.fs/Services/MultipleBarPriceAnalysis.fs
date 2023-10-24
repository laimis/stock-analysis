namespace core.fs.Services

open System.Collections.Generic
open core.fs.Services.Analysis
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

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
        
        let SMA interval = $"sma_%i{interval}"

    module MultipleBarPriceAnalysisConstants =
        let NumberOfDaysForRecentAnalysis = 60
        

    module SMAAnalysis =
        
        let private generateSMAOutcomes (smaContainer: SMAContainer) =
            
            let smaOutcomes = List<AnalysisOutcome>()
            
            for sma in smaContainer.All do
                
                let value =
                    match sma.LastValue with
                    | Some v ->
                        match v with
                        | Some v -> v
                        | None -> 0m
                    | None -> 0m
                    
                let outcome =
                    AnalysisOutcome(
                        MultipleBarOutcomeKeys.SMA(sma.Interval),
                        OutcomeType.Neutral,
                        value,
                        ValueFormat.Currency,
                        $"SMA {sma.Interval} is {value}"
                    )
                    
                smaOutcomes.Add outcome
                
            smaOutcomes
            
        let private generatePriceAboveSMA20Outcome (bars:PriceBar array) (container:SMAContainer) =
            
            let closingPrices = bars |> Array.map (fun b -> b.Close)
            
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
            )
            
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
            )
            
        let generate (prices: PriceBar array) =
            
            let smaContainer = SMAContainer.Generate prices
            
            let smaOutcomes = generateSMAOutcomes smaContainer
            
            smaContainer |> generateSMA20Above50DaysOutcome |> smaOutcomes.Add
            smaContainer |> generatePriceAboveSMA20Outcome prices |> smaOutcomes.Add
            
            smaOutcomes
       
    module VolumeAnalysis =
        
        let private generateAverageVolumeOutcome (prices: PriceBar array) =
            
            let recentVolumeStart, interval =
                match prices.Length with
                | x when x > MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis ->
                    prices.Length - MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis, MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis
                | _ -> 0, prices.Length
                
            let values = prices[recentVolumeStart..] |> Array.map (fun p -> p.Volume)
                
            let averageVolume = (values |> Array.sum) / (int64 values.Length)
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.AverageVolume,
                outcomeType = OutcomeType.Neutral,
                value = averageVolume,
                valueType = ValueFormat.Number,
                message = $"Average volume over the last {interval} days is {averageVolume}"
            )
            
        let generate (prices: PriceBar array) =
            
            let list = List<AnalysisOutcome>()
            prices |> generateAverageVolumeOutcome |> list.Add
            list

    module PriceAnalysis =
        
        let private recentBars (bars:PriceBar array) =
            match bars.Length with
                | x when x > MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis ->
                    bars[bars.Length - MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis..]
                | _ -> bars
                
        let private GenerateEarliestPriceOutcome (prices: PriceBar array) =
            
            let earliestPrice = prices[0]
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.EarliestPrice,
                outcomeType = OutcomeType.Neutral,
                value = earliestPrice.Close,
                valueType = ValueFormat.Currency,
                message = $"Earliest price was {earliestPrice.Close} on {earliestPrice.Date}"
            )
            
        let private GenerateLowestPriceOutcome (prices: PriceBar array) =
            
            let lowest = prices |> Array.minBy (fun p -> p.Close)
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.LowestPrice,
                outcomeType = OutcomeType.Neutral,
                value = lowest.Close,
                valueType = ValueFormat.Currency,
                message = $"Lowest price was {lowest.Close} on {lowest.Date}"
            )
            
        let private GenerateLowestPriceDaysAgoOutcome (prices: PriceBar array) =
            
            let lowest = prices |> Array.minBy (fun p -> p.Close)
            let lowestPriceDaysAgo = System.Math.Floor(System.DateTimeOffset.UtcNow.Subtract(lowest.Date).TotalDays)
            let lowestPriceDaysAgoOutcomeType = if lowestPriceDaysAgo <= 30 then OutcomeType.Negative else OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.LowestPriceDaysAgo,
                outcomeType = lowestPriceDaysAgoOutcomeType,
                value = (lowestPriceDaysAgo |> decimal),
                valueType = ValueFormat.Number,
                message = $"Lowest price was {lowest.Close} on {lowest.Date} which was {lowestPriceDaysAgo} days ago"
            )
            
        let private GeneratePercentAboveLowOutcome (currentPrice: decimal) (prices: PriceBar array) =
            
            let lowest = prices |> Array.minBy (fun p -> p.Close)
            let percentAboveLow = (currentPrice - lowest.Close) / lowest.Close
            let percentAboveLowOutcomeType = OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentAboveLow,
                outcomeType = percentAboveLowOutcomeType,
                value = percentAboveLow,
                valueType = ValueFormat.Percentage,
                message = $"Percent above recent low: {percentAboveLow}%%"
            )
            
        let private GenerateHighestPriceOutcome (prices: PriceBar array) =
            
            let highest = prices |> Array.maxBy (fun p -> p.Close)
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.HighestPrice,
                outcomeType = OutcomeType.Neutral,
                value = highest.Close,
                valueType = ValueFormat.Currency,
                message = $"Highest price was {highest.Close} on {highest.Date}"
            )
            
        let private GenerateHighestPriceDaysAgoOutcome (prices: PriceBar array) =
            
            let highest = prices |> Array.maxBy (fun p -> p.Close)
            let highestPriceDaysAgo = System.Math.Round(System.DateTimeOffset.UtcNow.Subtract(highest.Date).TotalDays, 0)
            let highestPriceDaysAgoOutcomeType = if highestPriceDaysAgo <= 30 then OutcomeType.Positive else OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.HighestPriceDaysAgo,
                outcomeType = highestPriceDaysAgoOutcomeType,
                value = (highestPriceDaysAgo |> decimal),
                valueType = ValueFormat.Number,
                message = $"Highest price was {highest.Close} on {highest.Date} which was {highestPriceDaysAgo} days ago"
            )
            
        let private GeneratePercentBelowHighOutcome (currentPrice: decimal) (prices: PriceBar array) =
            
            let highest = prices |> Array.maxBy (fun p -> p.Close)
            let percentBelowHigh = (highest.Close - currentPrice) / highest.Close
            let percentBelowHighOutcomeType = OutcomeType.Neutral
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentBelowHigh,
                outcomeType = percentBelowHighOutcomeType,
                value = percentBelowHigh,
                valueType = ValueFormat.Percentage,
                message = $"Percent below recent high: {percentBelowHigh}%%"
            )
            
        let private GenerateGainOutcome (currentPrice: decimal) (prices: PriceBar array) =
            
            let gain = (currentPrice - prices[0].Close) / prices[0].Close
            let gainOutcomeType = if gain > 0m then OutcomeType.Positive else OutcomeType.Negative
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.Gain,
                outcomeType = gainOutcomeType,
                value = gain,
                valueType = ValueFormat.Percentage,
                message = $"Gain from earliest to latest: {gain}%%"
            )
            
        let private GeneratePercentChangeAverageOutcome (prices: PriceBar array) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeAverage,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.mean,
                valueType = ValueFormat.Percentage,
                message = $"%% Change Average: {descriptor.mean}"
            )
            
        let private GeneratePercentChangeStandardDeviationOutcome (prices: PriceBar array) =
            
            let descriptor = prices |> recentBars |> PercentChangeAnalysis.calculateForPriceBars
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.PercentChangeStandardDeviation,
                outcomeType = OutcomeType.Neutral,
                value = descriptor.stdDev,
                valueType = ValueFormat.Number,
                message = $"%% Change StD: {descriptor.stdDev}"
            )
            
        let private GenerateCurrentPriceOutcome (currentPrice: decimal) =
            
            AnalysisOutcome(
                key = MultipleBarOutcomeKeys.CurrentPrice,
                outcomeType = OutcomeType.Neutral,
                value = currentPrice,
                valueType = ValueFormat.Currency,
                message = $"Current price is {currentPrice:C2}"
            )
            
        let Generate (currentPrice: decimal) (prices: PriceBar array) =
            
            let outcomes = List<AnalysisOutcome>()
            
            outcomes.Add (GenerateCurrentPriceOutcome currentPrice)
            outcomes.Add (GenerateEarliestPriceOutcome prices)
            outcomes.Add (GenerateLowestPriceOutcome prices)
            outcomes.Add (GenerateLowestPriceDaysAgoOutcome prices)
            outcomes.Add (GeneratePercentAboveLowOutcome currentPrice prices)
            outcomes.Add (GenerateHighestPriceOutcome prices)
            outcomes.Add (GenerateHighestPriceDaysAgoOutcome prices)
            outcomes.Add (GeneratePercentBelowHighOutcome currentPrice prices)
            outcomes.Add (GenerateGainOutcome currentPrice prices)
            outcomes.Add (GeneratePercentChangeAverageOutcome prices)
            outcomes.Add (GeneratePercentChangeStandardDeviationOutcome prices)
            
            outcomes
    
    module MultipleBarPriceAnalysis =
        
        let Run (currentPrice: decimal) prices =
            let outcomes = List<AnalysisOutcome>()
            
            prices |> PriceAnalysis.Generate currentPrice |> outcomes.AddRange
            prices |> VolumeAnalysis.generate |> outcomes.AddRange
            prices |> SMAAnalysis.generate |> outcomes.AddRange
            
            outcomes
            


    module MultipleBarAnalysisOutcomeEvaluation =
        let evaluate _ = []