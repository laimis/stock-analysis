module core.fs.Services.GapAnalysis

open core.fs.Adapters.Stocks
open core.fs.Services.Analysis

    
type GapType =
    | Up
    | Down
    
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
        RelativeVolume: decimal option
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
        
let private generateInternal (prices: PriceBars) (volumeStats: core.fs.Services.Analysis.DistributionStatistics) =
    
    prices.Bars
    |> Array.pairwise
    |> Array.filter (fun (previous, current) -> current.HasGap(previous))
    |> Array.indexed
    |> Array.map ( fun (index, (yesterday, current)) ->
        let gapSizePct =
            let referencePrice = min current.Open current.Close
            (referencePrice - yesterday.Close) / yesterday.Close
        
        let type' =
            match gapSizePct with
            | x when x > 0m -> GapType.Up
            | x when x < 0m -> GapType.Down
            | _ -> failwith $"Invalid gap type: {current} vs {yesterday}"
            
        let percentChange = (current.Close - yesterday.Close) / yesterday.Close
        
        let closingCondition =
            match gapSizePct with
            | x when x > 0m -> fun (bar:PriceBar) -> bar.Close <= yesterday.Close
            | x when x < 0m -> fun bar -> bar.Close >= yesterday.Close
            | _ -> failwith "Invalid gap type"
            
        let i = index * 2 // each indexed pair has two bars
        let closedQuickly = closingConditionMet prices.Bars i 10 closingCondition
        let open' = not (closingConditionMet prices.Bars i (prices.Length - i) closingCondition)
        let relativeVolume =
            match volumeStats.count < Constants.NumberOfDaysForRecentAnalysis with
            | false -> System.Math.Round ((current.Volume |> decimal) / volumeStats.mean, 2) |> Some
            | true -> None
            
        let closingRange = current.ClosingRange()
        
        {
            Type = type'
            GapSizePct = gapSizePct
            PercentChange = percentChange
            Bar = current
            ClosedQuickly = closedQuickly
            Open = open'
            RelativeVolume = relativeVolume
            ClosingRange = closingRange
        }
    )
    
let detectGaps (numberOfBarsToAnalyze: int) (prices: PriceBars) =
    
    let barsForAnalysis = numberOfBarsToAnalyze |> prices.LatestOrAll
    let barsForVolume = Constants.NumberOfDaysForRecentAnalysis |> prices.LatestOrAll 
    let volumeStats = barsForVolume.Volumes() |> DistributionStatistics.calculate
    
    generateInternal barsForAnalysis volumeStats
