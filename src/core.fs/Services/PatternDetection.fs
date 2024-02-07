module core.fs.Services.PatternDetection

open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis

let private toVolumeMultiplierString (multiplier: decimal option) =
    match multiplier with
    | Some m -> ", volume x" + m.ToString("N1")
    | None -> ""
    
let private relativeVolume (current:PriceBar) (bars:PriceBars) =
    match bars.Length >= Constants.NumberOfDaysForRecentAnalysis with
    | true ->
        let stats =
            Constants.NumberOfDaysForRecentAnalysis
            |> bars.LatestOrAll
            |> _.Volumes()
            |> DistributionStatistics.calculate
        
        decimal(current.Volume) / stats.median |> Some
    | false -> None
        
let gapDownName = "Gap Down"
let gapDown (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let gaps = bars |> detectGaps 2
        match gaps with
        | [|gap|] when gap.Type = GapType.Down ->
            let relativeVolume = gap.RelativeVolume |> toVolumeMultiplierString
            let gapPercentFormatted = System.Math.Round(gap.GapSizePct * 100m, 2)
            {
                date = bars.Last.Date
                name = gapDownName
                description = $"%s{gapDownName} {gapPercentFormatted}%%{relativeVolume}"
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
        let gaps = bars |> detectGaps 2
        match gaps with
        | [|gap|] when gap.Type = GapType.Up ->
            let relativeVolume = gap.RelativeVolume |> toVolumeMultiplierString
            let gapPercentFormatted = System.Math.Round(gap.GapSizePct * 100m, 2)
            {
                date = bars.Last.Date
                name = gapUpName
                description = $"%s{gapUpName} {gapPercentFormatted}%%{relativeVolume}"
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
            let volumeInfo = relativeVolume current bars |> toVolumeMultiplierString
                    
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
            let volumeInfo = relativeVolume current bars |> toVolumeMultiplierString
            
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
    
    match bars.Bars with
    | [||] -> None
    | _ ->
        let relativeVolume = relativeVolume bars.Last bars
        
        match relativeVolume with
        | None -> None
        | Some relativeVolume ->
            match relativeVolume with
            | x when x > 5m ->
                let bar = bars.Last
                Some({
                    date = bar.Date
                    name = highVolumeName
                    description = $"{highVolumeName}: " + bar.Volume.ToString("N0") + " (x" + relativeVolume.ToString("N1") + ")"
                    value = bar.Volume |> decimal
                    valueFormat = ValueFormat.Number
                    sentimentType = SentimentType.Neutral
                })
            | _ -> None
        
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

