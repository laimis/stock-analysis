namespace core.fs.Services

open System.Collections.Generic
open core.fs.Services.Analysis
open core.fs.Shared.Adapters.Stocks

module GapAnalysis =
    
    type GapType =
        | Up
        | Down
        
        with
            static member FromString(value:string) =
                match value with
                | nameof Up -> Up
                | nameof Down -> Down
                | _ -> failwith $"Invalid gap type: {value}"
                
            override this.ToString() =
                match this with
                | Up -> nameof Up
                | Down -> nameof Down
        
    type Gap =
        {
            Type: GapType
            GapSizePct: decimal
            PercentChange: decimal
            Bar: PriceBar
            ClosedQuickly: bool
            Open: bool
            RelativeVolume: decimal
            ClosingRange: decimal
        }
        
    let private closingConditionMet (prices: PriceBar array) (start: int) (length: int) (closingCondition: PriceBar -> bool) =
        
        let rec loop (i: int) =
            if i < start + length && i < prices.Length then
                if closingCondition prices[i] then
                    true
                else
                    loop (i + 1)
            else
                false
                
        loop start
        
    let private generateInternal (prices: PriceBars) (volumeStats: DistributionStatistics) =
        
        let gaps = List<Gap>()
        
        for i in 1 .. prices.Length - 1 do
            
            let yesterday = prices.Bars[i - 1]
            let currentBar = prices.Bars[i]
            
            let gapSizePct =
                if currentBar.Low > yesterday.High || currentBar.High < yesterday.Low then
                    // we take the lowest "significant" price of the day to calculate
                    // what the gap is.
                    // if it was a green day, then we care where it opened
                    // if it was a red day, we care where it closed
                    let referencePrice = min currentBar.Open currentBar.Close
                    (referencePrice - yesterday.Close) / yesterday.Close
                else
                    0m
                    
            if gapSizePct <> 0m then
                
                let type' =
                    match gapSizePct with
                    | x when x > 0m -> GapType.Up
                    | x when x < 0m -> GapType.Down
                    | _ -> failwith "Invalid gap type"
                    
                let percentChange = (currentBar.Close - yesterday.Close) / yesterday.Close
                
                let closingCondition =
                    match gapSizePct with
                    | x when x > 0m -> fun (bar:PriceBar) -> bar.Close <= yesterday.Close
                    | x when x < 0m -> fun bar -> bar.Close >= yesterday.Close
                    | _ -> failwith "Invalid gap type"
                    
                let closedQuickly = closingConditionMet prices.Bars (i + 1) 10 closingCondition
                let open' = not (closingConditionMet prices.Bars (i + 1) (prices.Length - i) closingCondition)
                let relativeVolume = System.Math.Round ((currentBar.Volume |> decimal) / volumeStats.mean, 2)
                let closingRange = currentBar.ClosingRange()
                
                let gap =
                    {
                        Type = type'
                        GapSizePct = gapSizePct
                        PercentChange = percentChange
                        Bar = currentBar
                        ClosedQuickly = closedQuickly
                        Open = open'
                        RelativeVolume = relativeVolume
                        ClosingRange = closingRange
                    }
                    
                gaps.Add gap
                
        gaps
        
    let detectGaps (prices: PriceBars) (numberOfBarsToAnalyze: int) =
        
        let barsForAnalysis = numberOfBarsToAnalyze |> prices.LatestOrAll
        
        let barsForVolume = numberOfBarsToAnalyze * 2 |> prices.LatestOrAll 
            
        let volumeStats = barsForVolume.Volumes() |> DistributionStatistics.calculate
        
        generateInternal barsForAnalysis volumeStats