module core.fs.Services.InflectionPoints

open core.fs.Adapters.Stocks
open MathNet.Numerics

// Domain types and models
type InfectionPointType = 
    | Peak
    | Valley
    override this.ToString() =
        match this with
        | Peak -> nameof Peak
        | Valley -> nameof Valley

    static member FromString s =
        match s with
        | nameof Peak -> Peak
        | nameof Valley -> Valley
        | _ -> failwith $"Invalid inflection point type {s}"

type Gradient = {
    Delta: decimal
    Index: int
    DataPoint: PriceBar
}

type InflectionPoint = {
    Gradient: Gradient
    Type: InfectionPointType
    PriceValue: decimal
}

type InflectionPointLog = {
    From: InflectionPoint
    To: InflectionPoint
    Days: float
    Change: decimal
    PercentChange: decimal
}

type TrendDirection =
    | Uptrend
    | Downtrend
    | Sideways
    | InsufficientData
    override this.ToString() =
        match this with
        | Uptrend -> nameof Uptrend
        | Downtrend -> nameof Downtrend
        | Sideways -> nameof Sideways
        | InsufficientData -> nameof InsufficientData

    static member FromString s =
        match s with
        | nameof Uptrend -> Uptrend
        | nameof Downtrend -> Downtrend
        | nameof Sideways -> Sideways
        | nameof InsufficientData -> InsufficientData
        | _ -> failwith $"Invalid trend type {s}"

type TrendDirectionAndStrength = {
    Direction: TrendDirection
    Strength: float
}

type TrendAnalysisDetails = {
    SlopeAnalysis: TrendDirectionAndStrength
    PatternAnalysis: TrendDirectionAndStrength
    RangeAnalysis: TrendDirectionAndStrength
    StrengthAnalysis: TrendDirectionAndStrength
}

type TrendAnalysisResult = {
    Trend: TrendDirection
    Confidence: float
    Details: TrendAnalysisDetails
}

type TrendChangeAlert = {
    Detected: bool
    Direction: TrendDirection
    Strength: float
    Evidence: string list
}

// Helper functions
let toPeak (gradient: Gradient): InflectionPoint =
    { Gradient = gradient; Type = Peak; PriceValue = gradient.DataPoint.Close }

let toValley (gradient: Gradient): InflectionPoint =
    { Gradient = gradient; Type = Valley; PriceValue = gradient.DataPoint.Close }

let filterBySignificance (points: InflectionPoint list) (minPercentChange: decimal) (minAge: int): InflectionPoint list =
    if points.Length <= 1 then 
        points
    else
        let rec filter (significant: InflectionPoint list) (remaining: InflectionPoint list) =
            match remaining with
            | [] -> significant
            | head::tail ->
                let lastPoint = significant |> List.last
                let percentChange = 
                    if lastPoint.PriceValue = 0M then
                        if head.PriceValue = 0M then 0M else 1M // If both are zero -> no change, otherwise significant change
                    else
                        System.Math.Abs((head.PriceValue - lastPoint.PriceValue) / lastPoint.PriceValue)
                let age = System.Math.Abs(head.Gradient.Index - lastPoint.Gradient.Index)
                
                if percentChange >= minPercentChange && age >= minAge then
                    filter (significant @ [head]) tail
                else
                    filter significant tail
                    
        filter [points.Head] points.Tail

let calculateVolatility (prices: PriceBar list) (window: int): float list =
    if prices.Length <= window then
        List.replicate prices.Length 0.0
    else
        let calculateWindow (index: int) =
            let priceChanges = 
                [index - window + 1 .. index]
                |> List.choose (fun i -> 
                    if i > 0 then 
                        Some (System.Math.Abs(float (prices[i].Close - prices[i-1].Close)))
                    else None)
                
            let mean = priceChanges |> List.average
            let squaredDiffs = priceChanges |> List.map (fun val' -> (val' - mean) ** 2.0)
            let variance = squaredDiffs |> List.sum |> fun sum -> sum / float squaredDiffs.Length
            System.Math.Sqrt variance
            
        let volatilities = 
            [window .. prices.Length - 1]
            |> List.map calculateWindow
            
        let firstVol = 
            match volatilities with
            | [] -> 0.0
            | head::_ -> head
            
        List.replicate window firstVol @ volatilities

let areIncreasing (values: decimal list): bool =
    values 
    |> List.pairwise 
    |> List.forall (fun (a, b) -> b > a)

let areDecreasing (values: decimal list): bool =
    values 
    |> List.pairwise 
    |> List.forall (fun (a, b) -> b < a)

// The main inflection point calculation functions
let calculateInflectionPointsFixedSmoothing (prices: PriceBar array) (smoothingFactor: float): InflectionPoint list =
    // First calculate price differences
    let diffs = 
        [0 .. prices.Length - 2]
        |> List.map (fun i ->
            let diff = prices[i+1].Close - prices[i].Close
            { Delta = diff; Index = i; DataPoint = prices[i] })
    
    // Apply fixed smoothing factor
    let rec smooth (acc: Gradient list) (remaining: Gradient list) (index: int) =
        match remaining with
        | [] -> acc
        | current::rest ->
            if index = 0 || index = diffs.Length - 1 then
                smooth (acc @ [current]) rest (index + 1)
            else
                let prevSmoothed = 
                    if acc.Length > 0 then acc[acc.Length - 1].Delta
                    else diffs[index-1].Delta
                
                let currentDiff = current.Delta
                let smoothedDiff = decimal(float currentDiff * (1.0 - smoothingFactor) + float prevSmoothed * smoothingFactor)
                
                let smoothedGradient = { current with Delta = smoothedDiff }
                smooth (acc @ [smoothedGradient]) rest (index + 1)
    
    let smoothed = smooth [] diffs 0
    
    // Detect inflection points
    let inflectionPoints =
        smoothed
        |> List.pairwise
        |> List.choose (fun (a, b) ->
            if a.Delta > 0M && b.Delta < 0M then
                Some(toPeak(b))
            elif a.Delta < 0M && b.Delta > 0M then
                Some(toValley(b))
            else
                None)
    
    filterBySignificance inflectionPoints 0.03M 5

// Main function to calculate inflection points
let calculateInflectionPoints (prices: PriceBar array): InflectionPoint list =
    calculateInflectionPointsFixedSmoothing prices 0.5


// Trend analysis functions
let private linearRegressionAnalysis (inflectionPoints: InflectionPoint list) (minPoints: int): TrendDirectionAndStrength =
    if inflectionPoints.Length < minPoints then 
        { Direction = InsufficientData; Strength = 0.0 }
    else
        // Extract recent points
        let recentPoints = inflectionPoints |> List.skip (inflectionPoints.Length - minPoints)
        
        // Prepare data for regression
        let x = recentPoints |> List.mapi (fun i _ -> float i)
        let y = recentPoints |> List.map (fun p -> float p.PriceValue)
        
        // Calculate linear regression slope
        let struct (_, slope) = Fit.Line(x |> List.toArray, y |> List.toArray)
        let threshold = 0.005
        
        if slope > threshold then 
            { Direction = Uptrend; Strength = 1.0 }
        elif slope < -threshold then 
            { Direction = Downtrend; Strength = 1.0 }
        else 
            { Direction = Sideways; Strength = 1.0 }

let private analyzePeaksAndValleys (inflectionPoints: InflectionPoint list) (lookback: int): TrendDirectionAndStrength =
    if inflectionPoints.Length < lookback * 2 then 
        { Direction = InsufficientData; Strength = 0.0 }
    else
        // Extract peaks and valleys
        let peaks = 
            inflectionPoints 
            |> List.filter (fun p -> p.Type = Peak)
            |> List.rev
            |> List.truncate lookback
            |> List.rev
            
        let valleys = 
            inflectionPoints 
            |> List.filter (fun p -> p.Type = Valley)
            |> List.rev
            |> List.truncate lookback
            |> List.rev
        
        // Check if we have at least 2 peaks and 2 valleys
        if peaks.Length < 2 || valleys.Length < 2 then 
            { Direction = InsufficientData; Strength = 0.0 }
        else
            // Check for higher highs and higher lows (uptrend)
            let higherHighs = areIncreasing (peaks |> List.map (fun p -> p.PriceValue))
            let higherLows = areIncreasing (valleys |> List.map (fun p -> p.PriceValue))
            
            // Check for lower highs and lower lows (downtrend)
            let lowerHighs = areDecreasing (peaks |> List.map (fun p -> p.PriceValue))
            let lowerLows = areDecreasing (valleys |> List.map (fun p -> p.PriceValue))
            
            match higherHighs, higherLows, lowerHighs, lowerLows with
            | true, true, _, _ -> { Direction = Uptrend; Strength = 1.0 }
            | _, _, true, true -> { Direction = Downtrend; Strength = 1.0 }
            | true, _, _, _ | _, true, _, _ -> { Direction = Uptrend; Strength = 0.5 }
            | _, _, true, _ | _, _, _, true -> { Direction = Downtrend; Strength = 0.5 }
            | _ -> { Direction = Sideways; Strength = 0.0 }

let private analyzeRange (inflectionPoints: InflectionPoint list): TrendDirectionAndStrength =
    if inflectionPoints.Length < 4 then 
        { Direction = InsufficientData; Strength = 0.0 }
    else
        // Find min and max prices
        let prices = inflectionPoints |> List.map (fun p -> p.PriceValue)
        let minPrice = prices |> List.min
        let maxPrice = prices |> List.max
        
        // Calculate the range as a percentage of the average price
        let range = maxPrice - minPrice
        let avgPrice = prices |> List.average
        let rangePercent = range / avgPrice
        
        // If range is small relative to price, it's sideways
        if rangePercent < 0.05M then 
            { Direction = Sideways; Strength = 1.0 }
        else
            // Check position of latest price within the range
            let latestPrice = (inflectionPoints |> List.last).PriceValue
            let positionInRange = float ((latestPrice - minPrice) / range)
            
            if positionInRange > 0.7 then 
                { Direction = Uptrend; Strength = 1.0 }  // Near the top of range
            elif positionInRange < 0.3 then 
                { Direction = Downtrend; Strength = 1.0 }  // Near the bottom of range
            else 
                { Direction = Sideways; Strength = 1.0 }  // In middle of range

let private calculateTrendStrength (inflectionPoints: InflectionPoint list) (numberOfPoints: int): TrendDirectionAndStrength =
    if inflectionPoints.Length < numberOfPoints then 
        { Direction = InsufficientData; Strength = 0.0 }
    else
        // Extract recent points
        let recentPoints = inflectionPoints |> List.skip (inflectionPoints.Length - numberOfPoints)
        
        // 1. Calculate linear regression slope
        let x = recentPoints |> List.mapi (fun i _ -> float i)
        let y = recentPoints |> List.map (fun p -> float p.PriceValue)
        
        let struct (_, slope) = Fit.Line(x |> List.toArray, y |> List.toArray)
        
        // 2. Check for higher highs/lower lows
        let peaks = recentPoints |> List.filter (fun p -> p.Type = Peak)
        let valleys = recentPoints |> List.filter (fun p -> p.Type = Valley)
        
        let mutable patternScore = 0.0
        
        if peaks.Length >= 2 then
            // Check if peaks are increasing (uptrend) or decreasing (downtrend)
            let peakDiff = peaks[peaks.Length-1].PriceValue - peaks[0].PriceValue
            patternScore <- patternScore + (if peakDiff > 0M then 1.0 else if peakDiff < 0M then -1.0 else 0.0)
        
        if valleys.Length >= 2 then
            // Check if valleys are increasing (uptrend) or decreasing (downtrend)
            let valleyDiff = valleys[valleys.Length-1].PriceValue - valleys[0].PriceValue
            patternScore <- patternScore + (if valleyDiff > 0M then 1.0 else if valleyDiff < 0M then -1.0 else 0.0)
        
        // 3. Calculate total percentage change
        let totalChange = 
            float ((recentPoints[recentPoints.Length-1].PriceValue - recentPoints[0].PriceValue) / recentPoints[0].PriceValue)
        
        // 4. Normalize and combine factors
        let normalizedSlope = System.Math.Min(System.Math.Max(slope * 20.0, -1.0), 1.0)
        let normalizedChange = System.Math.Min(System.Math.Max(totalChange * 10.0, -1.0), 1.0)
        let normalizedPattern = patternScore / 2.0
        
        // Combine scores
        let combinedScore = (normalizedSlope + normalizedChange + normalizedPattern) / 3.0
        
        // Convert to trend and strength
        let direction =
            if combinedScore > 0.2 then Uptrend
            elif combinedScore < -0.2 then Downtrend
            else Sideways
        
        // Return both trend and strength
        { Direction = direction; Strength = System.Math.Abs combinedScore }

let analyzeTrend (inflectionPoints: InflectionPoint list): TrendAnalysisResult =
    if inflectionPoints.Length < 8 then
        { 
            Trend = InsufficientData
            Confidence = 0.0
            Details = {
                SlopeAnalysis = { Direction = InsufficientData; Strength = 0.0 }
                PatternAnalysis = { Direction = InsufficientData; Strength = 0.0 }
                RangeAnalysis = { Direction = InsufficientData; Strength = 0.0 }
                StrengthAnalysis = { Direction = InsufficientData; Strength = 0.0 }
            }
        }
    else
        // Get results from different methods
        let slopeResult = linearRegressionAnalysis inflectionPoints 8
        let patternResult = analyzePeaksAndValleys inflectionPoints 4
        let rangeResult = analyzeRange inflectionPoints
        let strengthResult = calculateTrendStrength inflectionPoints 8
        
        // Vote for the final trend
        let mutable upVotes = 0.0
        let mutable downVotes = 0.0
        let mutable sidewaysVotes = 0.0
        
        let results = [slopeResult; patternResult; rangeResult; strengthResult]
        
        for result in results do
            match result.Direction with
            | Uptrend -> upVotes <- upVotes + result.Strength
            | Downtrend -> downVotes <- downVotes + result.Strength
            | _ -> sidewaysVotes <- sidewaysVotes + result.Strength
        
        let numberOfMethods = 4.0
        let finalTrend, confidence =
            if upVotes > downVotes && upVotes > sidewaysVotes then
                Uptrend, upVotes
            elif downVotes > upVotes && downVotes > sidewaysVotes then
                Downtrend, downVotes
            else
                Sideways, sidewaysVotes
        
        // Adjust confidence by trend strength
        let adjustedConfidence = (confidence / numberOfMethods + strengthResult.Strength) / 2.0
        
        {
            Trend = finalTrend
            Confidence = adjustedConfidence
            Details = {
                SlopeAnalysis = slopeResult
                PatternAnalysis = patternResult
                RangeAnalysis = rangeResult
                StrengthAnalysis = strengthResult
            }
        }

let detectPotentialTrendChange
    (inflectionPoints: InflectionPoint list)
    (latestPrice: decimal)
    (latestBar: PriceBar): TrendChangeAlert =
    
    if inflectionPoints.Length < 4 then
        { 
            Detected = false
            Direction = InsufficientData
            Strength = 0.0
            Evidence = ["Insufficient data"]
        }
    else
        // Extract recent peaks and valleys
        let recentPeaks = 
            inflectionPoints
            |> List.filter (fun p -> p.Type = Peak)
            |> List.rev
            |> List.truncate 3
            |> List.rev
        
        let recentValleys = 
            inflectionPoints
            |> List.filter (fun p -> p.Type = Valley)
            |> List.rev
            |> List.truncate 3
            |> List.rev
        
        let lastInflectionPoint = inflectionPoints |> List.last
        let daysSinceLastInflection = 
            (System.DateTime.Parse latestBar.DateStr - System.DateTime.Parse lastInflectionPoint.Gradient.DataPoint.DateStr).TotalDays
        
        let peaksExceededCheck() =
            let peaksExceeded = recentPeaks |> List.filter (fun p -> latestPrice > p.PriceValue)
            match peaksExceeded with
            | [] -> None
            | _ -> 
                (
                    $"BULLISH: Price ({float latestPrice:F2}) exceeds {peaksExceeded.Length} of the last {recentPeaks.Length} peaks",
                    float peaksExceeded.Length / float recentPeaks.Length * 0.5
                ) |> Some

        let riseFromRecentValleyCheck() =
            match recentValleys |> List.tryLast with
            | None -> None
            | Some lastValley ->
                let percentRise = float ((latestPrice - lastValley.PriceValue) / lastValley.PriceValue)
                if percentRise > 0.05 then // 5% rise from valley
                    Some (
                        $"BULLISH: Price has risen %.1f{(percentRise * 100.0)}%% from last valley",
                        System.Math.Min(percentRise, 0.2) * 2.5 // Cap at 0.5 contribution
                    )
                else None
            
        let higherLowsCheck() =
            let increasing = areIncreasing (recentValleys |> List.map (fun v -> v.PriceValue))
            match increasing && recentValleys.Length >= 2 with
            | true -> Some ("BULLISH: Pattern of higher lows detected", 0.2)
            | _ -> None

        let bullishFactors = [peaksExceededCheck(); riseFromRecentValleyCheck(); higherLowsCheck()] |> List.choose id
        let bullishStrength = bullishFactors |> List.fold (fun acc (_, strength) -> acc + strength) 0.0
        let bullishEvidence = bullishFactors |> List.map fst
        
        
        // bearish checks
        let valleysExceededCheck() =
            let valleysExceeded = recentValleys |> List.filter (fun v -> latestPrice < v.PriceValue)
            match valleysExceeded with
            | [] -> None
            | _ -> 
                (
                    $"BEARISH: Price ({float latestPrice:F2}) is below {valleysExceeded.Length} of the last {recentValleys.Length} valleys",
                    float valleysExceeded.Length / float recentValleys.Length * 0.5
                ) |> Some
        
        let fallFromRecentPeakCheck() =
            match recentPeaks |> List.tryLast with
            | None -> None
            | Some lastPeak ->
                let percentFall = float ((lastPeak.PriceValue - latestPrice) / lastPeak.PriceValue)
                if percentFall > 0.05 then // 5% fall from peak
                    Some (
                        $"BEARISH: Price has fallen %.1f{percentFall * 100.0}%% from last peak",
                        System.Math.Min(percentFall, 0.2) * 2.5
                    )
                else None
        
        let lowerHighsCheck() =
            let decreasing = areDecreasing (recentPeaks |> List.map (fun p -> p.PriceValue))
            match decreasing && recentPeaks.Length >= 2 with
            | true -> Some ("BEARISH: Pattern of lower highs detected", 0.2)
            | _ -> None

        let bearishFactors = [valleysExceededCheck(); fallFromRecentPeakCheck(); lowerHighsCheck()] |> List.choose id
        let bearishStrength = bearishFactors |> List.fold (fun acc (_, strength) -> acc + strength) 0.0
        let bearishEvidence = bearishFactors |> List.map fst
        
        // Only set a direction if one signal is significantly stronger
        let SIGNIFICANCE_THRESHOLD = 0.2
        
        // Determine final direction and strength
        let directionSignal, netStrength =
            if bullishStrength > bearishStrength + SIGNIFICANCE_THRESHOLD then
                Uptrend, bullishStrength
            elif bearishStrength > bullishStrength + SIGNIFICANCE_THRESHOLD then
                Downtrend, bearishStrength
            else
                // If signals are contradictory and similar in strength, it's unclear
                if bullishStrength > 0.3 && bearishStrength > 0.3 then
                    Sideways, System.Math.Max(bullishStrength, bearishStrength) * 0.5
                else
                    let netStrength = System.Math.Max(bullishStrength, bearishStrength)
                    if bullishStrength >= bearishStrength then
                        Uptrend, netStrength
                    else
                        Downtrend, netStrength

        let evidence =
            bullishEvidence @ bearishEvidence
            @ [
                if daysSinceLastInflection > 10.0 then
                    $"NEUTRAL: {System.Math.Floor daysSinceLastInflection} days since last inflection point (unusual duration)"
                if bullishStrength > 0.0 || bearishStrength > 0.0 then
                    $"Signal strength: Bullish=%.2f{bullishStrength}, Bearish=%.2f{bearishStrength}"
                if bullishStrength > 0.3 && bearishStrength > 0.3 then
                    "CONFLICT: Strong but contradictory signals detected"
            ]
        
        // Determine if signal is strong enough
        let detected = netStrength >= 0.4
        
        {
            Detected = detected
            Direction = directionSignal
            Strength = System.Math.Min(netStrength, 1.0) // Cap at 1
            Evidence = evidence
        }

let getCompleteTrendAnalysis 
    (inflectionPoints: InflectionPoint list)
    (latestBar: PriceBar) = 
    
    let trendAnalysis = analyzeTrend inflectionPoints
    let changeAlert = detectPotentialTrendChange inflectionPoints latestBar.Close latestBar
    
    {| 
        EstablishedTrend = trendAnalysis
        PotentialChange = changeAlert 
    |}