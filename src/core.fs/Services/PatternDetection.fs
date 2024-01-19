module core.fs.Services.PatternDetection

open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Services.Analysis.SingleBarPriceAnalysis
open core.fs.Services.GapAnalysis

let gapDownName = "Gap Down"
let gapDown (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let gaps = detectGaps bars 2
        match gaps with
        | [|gap|] when gap.Type = GapType.Down ->
            let relativeVolume = gap.RelativeVolume.ToString("N1")
            let gapPercentFormatted = System.Math.Round(gap.GapSizePct * 100m, 2)
            {
                date = bars.Last.Date
                name = gapDownName
                description = $"%s{gapDownName} {gapPercentFormatted}%%, volume x{relativeVolume}"
                value = gap.GapSizePct
                valueFormat = ValueFormat.Percentage
                sentimentType = SentimentType.Negative 
            } |> Some
        | _ -> None

let gapUpName = "Gap Up"
let gapUp (bars: PriceBars) =
    
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let gaps = detectGaps bars 2
        match gaps with
        | [|gap|] when gap.Type = GapType.Up ->
            let relativeVolume = gap.RelativeVolume.ToString("N1")
            let gapPercentFormatted = System.Math.Round(gap.GapSizePct * 100m, 2)
            {
                date = bars.Last.Date
                name = gapUpName
                description = $"%s{gapUpName} {gapPercentFormatted}%%, volume x{relativeVolume}"
                value = gap.GapSizePct
                valueFormat = ValueFormat.Percentage
                sentimentType = SentimentType.Positive 
            } |> Some
        | _ -> None

let downsideReversalName = "Downside Reversal"
let downsideReversal (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let current = bars.Last
        let previous = bars.Bars[bars.Length - 2]
        
        // downside reversal pattern detection
        if current.Close < System.Math.Min(previous.Open, previous.Close) && current.High > previous.High then
            let completeReversal = current.Close < previous.Low
            let volumeInfo = 
                // see if we can do volume numbers
                if bars.Length >= Constants.NumberOfDaysForRecentAnalysis then
                    
                    let stats =
                        Constants.NumberOfDaysForRecentAnalysis
                        |> bars.LatestOrAll
                        |> _.Volumes()
                        |> DistributionStatistics.calculate
                    
                    let multiplier = decimal(current.Volume) / stats.median
                    ", volume x" + multiplier.ToString("N1")
                else
                    ""
                    
            let additionalInfo =
                match completeReversal with
                | true -> " (complete) " + volumeInfo
                | false -> volumeInfo
                
            {
                date = current.Date
                name = downsideReversalName
                description = $"{downsideReversalName}{additionalInfo}"
                value = current.Close
                valueFormat = ValueFormat.Currency
                sentimentType = SentimentType.Negative 
            } |> Some
        else
            None

let upsideReversalName = "Upside Reversal"
let upsideReversal (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let current = bars.Last
        let previous = bars.Bars[bars.Length - 2]
        
        // upside reversal pattern detection
        if current.Close > System.Math.Max(previous.Open, previous.Close) && current.Low < previous.Low then
            let completeReversal = current.Close < previous.Low
            let volumeInfo = 
                // see if we can do volume numbers
                if bars.Length >= Constants.NumberOfDaysForRecentAnalysis then
                    
                    let stats =
                        Constants.NumberOfDaysForRecentAnalysis
                        |> bars.LatestOrAll
                        |> _.Volumes()
                        |> DistributionStatistics.calculate
                    
                    let multiplier = decimal(current.Volume) / stats.median
                    ", volume x" + multiplier.ToString("N1")
                else
                    ""
            
            let additionalInfo =
                match completeReversal with
                | true -> " (complete) " + volumeInfo
                | false -> volumeInfo
                
            {
                date = current.Date
                name = upsideReversalName
                description = $"{upsideReversalName}{additionalInfo}"
                value = current.Close
                valueFormat = ValueFormat.Currency
                sentimentType = SentimentType.Positive 
            } |> Some
        else
            None


let highest1YearVolumeName = "Highest 1 year volume"

let highest1YearVolume (bars: PriceBars) =
    if bars.Length < 2 then
        None
    else
        // find the starting bar, which is higher date than 1 year ago
        let thresholdDate = System.DateTimeOffset.UtcNow.AddYears(-1)
        let startIndex = 
            bars.Bars
            |> Array.indexed
            |> Array.tryFind (fun (_, b) -> b.Date < thresholdDate)
            |> Option.map (fun (i, _) -> i)
            |> Option.defaultValue (bars.Length - 1)
        
        // we couldn't find a bar that's older than 1 year, so no pattern
        if bars.Bars[startIndex].Date > thresholdDate then
            None
        else
            // now examine all bars from the starting bar to the end
            // and see if the last one has the highest volume
            let highestVolume = 
                bars.Bars
                |> Array.skip startIndex
                |> Array.map (fun b -> b.Volume)
                |> Array.max
                
            if highestVolume = bars.Last.Volume then
                let bar = bars.Last
                Some({
                    date = bar.Date
                    name = highest1YearVolumeName
                    description = $"{highest1YearVolumeName}: " + bar.Volume.ToString("N0")
                    value = bar.Volume |> decimal
                    valueFormat = ValueFormat.Number
                    sentimentType = SentimentType.Neutral 
                })
            else
                None
                
let highVolumeName = "High Volume"

let highVolume (bars: PriceBars) =
    if bars.Length < Constants.NumberOfDaysForRecentAnalysis then
        None
    else
        let subsetOfBars = bars.LatestOrAll Constants.NumberOfDaysForRecentAnalysis
            
        let volumeStats = subsetOfBars.Volumes() |> DistributionStatistics.calculate
        
        // now take the last bar volume
        let lastBarVolume = bars.Last.Volume
        let multiplier = decimal(lastBarVolume) / volumeStats.median
        
        // if the last bar volume is some predefined multiple of the average volume, then we have a pattern
        let threshold = volumeStats.median * decimal(5) |> int64
        
        if lastBarVolume > threshold then
            let bar = bars.Last
            Some({
                date = bar.Date
                name = highVolumeName
                description = $"{highVolumeName}: " + bar.Volume.ToString("N0") + " (x" + multiplier.ToString("N1") + ")"
                value = bar.Volume |> decimal
                valueFormat = ValueFormat.Number
                sentimentType = SentimentType.Neutral
            })
        else
            None
            
let patternGenerators =
    
    [
        upsideReversalName, upsideReversal
        downsideReversalName, downsideReversal
        highest1YearVolumeName, highest1YearVolume
        highVolumeName, highVolume
        gapUpName, gapUp
        gapDownName, gapDown
    ]
    |> Map.ofList

let availablePatterns =
    patternGenerators
    |> Map.toSeq
    |> Seq.map (fun (name, _) -> name)
    |> Seq.toList        
    
let generate (bars: PriceBars) =
    
    patternGenerators
    |> Map.toSeq
    |> Seq.map (fun (_, generator) -> generator bars)
    |> Seq.choose id
    |> Seq.toList

