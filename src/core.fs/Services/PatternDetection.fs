module core.fs.Services.PatternDetection

open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis

[<Literal>]
let contractingVolumeBreakoutName = "Contracting Volume Breakout"
[<Literal>]
let maximumBarsForPattern = 40
let volumeRateThreshold = 2m
let slopeThreshold = 0.0

let contractingVolumeBreakout (bars: PriceBars) =
    
    if bars.Length < 10 then
        None
    else
        let barsOfInterest = // don't look too far back, just the last 40 bars unless there are less than 40 bars
            match bars.Length with
            | x when x < maximumBarsForPattern -> bars.Bars
            | _ -> bars.Bars[^(maximumBarsForPattern-1)..]
        
        let volumes = barsOfInterest |> Array.map _.Volume
        let volumeStats = 
            volumes
            |> Array.map decimal
            |> DistributionStatistics.calculate
            
        let lastBar = bars.Last
        let lastVolume = decimal lastBar.Volume
        let lastVolumeRate = lastVolume / volumeStats.mean
        
        let x = [|0.0..(volumes.Length - 1 |> float)|]
        let yVol = volumes |> Array.map float
        let xScale = volumes.Length - 1 |> float
        let yVolScale = Array.max yVol - Array.min yVol
    
        let struct (_, volSlope) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(x, yVol)
        let adjustedVolSlope = volSlope * xScale / yVolScale
        let angleVolDegrees = System.Math.Atan(adjustedVolSlope) * 180.0 / System.Math.PI
        
        let closes = barsOfInterest |> Array.map (fun bar -> bar.Close |> float)
        let struct (_, closeSlope) = MathNet.Numerics.Fit.Line(x, closes)
        let yCloseScale = Array.max closes - Array.min closes
        let adjustedCloseSlope = closeSlope * xScale / yCloseScale
        let angleCloseDegrees = System.Math.Atan(adjustedCloseSlope) * 180.0 / System.Math.PI
       
        let description = $"{contractingVolumeBreakoutName}: vr: {lastVolumeRate:N1}x, va: {angleVolDegrees:N2}°, pa: {angleCloseDegrees:N2}°"
            
        if lastVolumeRate >= volumeRateThreshold && volSlope < slopeThreshold && (lastBar.Close > lastBar.Open) then
            Some({
                date = lastBar.Date
                name = contractingVolumeBreakoutName
                description = description
                value = lastVolumeRate
                valueFormat = ValueFormat.Number
                sentimentType = SentimentType.Positive
            })
        else
            None
            
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
        
let gap (gapType: GapType) (name: string) (sentimentType: SentimentType) (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let gaps = bars |> detectGaps 2
        match gaps with
        | [|gap|] when gap.Type = gapType ->
            let relativeVolume = gap.RelativeVolume |> toVolumeMultiplierString
            let valueToUse = gap.PercentChange
            let gapPercentFormatted = System.Math.Round(valueToUse * 100m, 2)
            {
                date = bars.Last.Date
                name = name
                description = $"%s{name} {gapPercentFormatted}%%{relativeVolume}"
                value = valueToUse
                valueFormat = ValueFormat.Percentage
                sentimentType = sentimentType
            } |> Some
        | _ -> None

[<Literal>]
let gapUpName = "Gap Up"
[<Literal>]
let gapDownName = "Gap Down"
let gapDown = gap GapType.Down gapDownName SentimentType.Negative
let gapUp = gap GapType.Up gapUpName SentimentType.Positive

let private reversal (isReversal: PriceBar -> PriceBar -> bool) (sentimentType: SentimentType) (name: string) (bars: PriceBars) =
    match bars.Length with
    | 0 | 1 -> None
    | _ ->
        let current = bars.Last
        let previous = bars.Bars[bars.Length - 2]

        // reversal pattern detection
        if isReversal current previous then
            let volumeInfo = relativeVolume current bars |> toVolumeMultiplierString

            {
                date = current.Date
                name = name
                description = $"{name}{volumeInfo}"
                value = current.Close
                valueFormat = ValueFormat.Currency
                sentimentType = sentimentType
            } |> Some
        else
            None

[<Literal>]
let upsideReversalName = "Upside Reversal"
let upsideReversal =
    let isUpsideReversal (current:PriceBar) (previous:PriceBar) = current.Close > previous.High && current.Low < previous.Low
    reversal isUpsideReversal SentimentType.Positive upsideReversalName

[<Literal>]
let downsideReversalName = "Downside Reversal"
let downsideReversal =
    let isDownsideReversal (current:PriceBar) (previous:PriceBar) = current.Close < previous.Low && current.High > previous.High
    reversal isDownsideReversal SentimentType.Negative downsideReversalName
    
[<Literal>]
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
                
[<Literal>]
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
        contractingVolumeBreakoutName, contractingVolumeBreakout
    ]

let availablePatterns =
    patternGenerators
    |> List.map fst      
    
let generate (bars: PriceBars) =
    
    patternGenerators
    |> List.map (fun (_, generator) -> generator bars)
    |> List.choose id

