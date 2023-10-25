namespace core.fs.Services

open core.Shared
open core.fs.Services.Analysis
open core.fs.Services.Analysis.SingleBarPriceAnalysis
open core.fs.Services.GapAnalysis
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

module PatternDetection =
    
    let gapUpName = "Gap Up"
    
    let gapUp (bars: PriceBar[]) =
        if bars.Length < 2 then
            None
        else
            let gaps = detectGaps bars 2
            if gaps.Count = 0 || gaps[0].Type <> GapType.Up then
                None
            else
                let gap = gaps[0]
                let gapPercentFormatted =
                    System.Math.Round(
                        gap.GapSizePct * 100m,
                        2
                    )
                Some({
                    date = bars[bars.Length - 1].Date
                    name = gapUpName
                    description = $"%s{gapUpName} {gapPercentFormatted}%%"
                    value = gap.GapSizePct
                    valueFormat = ValueFormat.Percentage
                })
                
    let upsideReversalName = "Upside Reversal"
    
    let upsideReversal (bars: PriceBar[]) =
        if bars.Length < 2 then
            None
        else
            let current = bars[bars.Length - 1]
            let previous = bars[bars.Length - 2]
            
            // upside reversal pattern detection
            if current.Close > System.Math.Max(previous.Open, previous.Close) && current.Low < previous.Low then
                let additionalInfo = 
                // see if we can do volume numbers
                    if bars.Length >= SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis then
                        
                        let stats =
                            bars[bars.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis..]
                            |> Array.map (fun b -> b.Volume |> decimal)
                            |> DistributionStatistics.calculate
                        
                        let multiplier = decimal(current.Volume) / stats.median
                        ", volume x" + multiplier.ToString("N1")
                    else
                        ""
                    
                Some({
                    date = current.Date
                    name = upsideReversalName
                    description = $"{upsideReversalName}{additionalInfo}"
                    value = current.Close
                    valueFormat = ValueFormat.Currency
                })
            else
                None
    
    
    let highest1YearVolumeName = "Highest 1 year volume"
    
    let highest1YearVolume (bars: PriceBar[]) =
        if bars.Length < 2 then
            None
        else
            // find the starting bar, which is higher date than 1 year ago
            let thresholdDate = System.DateTimeOffset.UtcNow.AddYears(-1)
            let startIndex = 
                bars
                |> Array.indexed
                |> Array.tryFind (fun (_, b) -> b.Date < thresholdDate)
                |> Option.map (fun (i, _) -> i)
                |> Option.defaultValue (bars.Length - 1)
            
            // we couldn't find a bar that's older than 1 year, so no pattern
            if bars[startIndex].Date > thresholdDate then
                None
            else
                // now examine all bars from the starting bar to the end
                // and see if the last one has the highest volume
                let highestVolume = 
                    bars
                    |> Array.skip startIndex
                    |> Array.map (fun b -> b.Volume)
                    |> Array.max
                    
                if highestVolume = bars[bars.Length - 1].Volume then
                    let bar = bars[bars.Length - 1]
                    Some({
                        date = bar.Date
                        name = highest1YearVolumeName
                        description = $"{highest1YearVolumeName}: " + bar.Volume.ToString("N0")
                        value = bar.Volume |> decimal
                        valueFormat = ValueFormat.Number
                    })
                else
                    None
                    
    let highVolumeName = "High Volume"
    
    let highVolume (bars: PriceBar[]) =
        if bars.Length < SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis then
            None
        else
            let subsetOfBars =
                bars[bars.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis..]
                
            let volumeStats = subsetOfBars |> Array.map (fun b -> b.Volume |> decimal) |> DistributionStatistics.calculate
            
            // now take the last bar volume
            let lastBarVolume = bars[bars.Length - 1].Volume
            let multiplier = decimal(lastBarVolume) / volumeStats.median
            
            // if the last bar volume is some predefined multiple of the average volume, then we have a pattern
            let threshold = volumeStats.median * decimal(5) |> int64
            
            if lastBarVolume > threshold then
                let bar = bars[bars.Length - 1]
                Some({
                    date = bar.Date
                    name = highVolumeName
                    description = $"{highVolumeName}: " + bar.Volume.ToString("N0") + " (x" + multiplier.ToString("N1") + ")"
                    value = bar.Volume |> decimal
                    valueFormat = ValueFormat.Number
                })
            else
                None
                
    let patternGenerators =
        
        [
            upsideReversalName, upsideReversal
            highest1YearVolumeName, highest1YearVolume
            highVolumeName, highVolume
            gapUpName, gapUp
        ]
        |> Map.ofList

    let availablePatterns =
        patternGenerators
        |> Map.toSeq
        |> Seq.map (fun (name, _) -> name)
        |> Seq.toList        
        
    let generate (bars: PriceBar[]) =
        
        patternGenerators
        |> Map.toSeq
        |> Seq.map (fun (_, generator) -> generator bars)
        |> Seq.choose id
        |> Seq.toList

