module coretests.fs.Services.InflectionPointsTests

open FsUnit
open Xunit
open core.fs.Services.InflectionPoints
open core.Shared
open core.fs.Adapters.Stocks

let getPriceBars ticker =
    testutils.TestDataGenerator.PriceBars ticker

let uptrendPrices =
    "RBRK" |> Ticker |> getPriceBars |> fun f -> f.Bars

let downtrendPrices =
    testutils.TestDataGenerator.NET |> getPriceBars |> fun f -> f.Bars

// Tests for inflection point calculation
[<Fact>]
let ``calculateInflectionPoints finds peaks and valleys`` () =
    let points = 
        testutils.TestDataGenerator.NET |> getPriceBars |> fun f -> f.Bars |> calculateInflectionPoints
    
    // Should identify 3 peaks and 2 valleys
    let peaks = points |> List.filter (fun p -> p.Type = Peak)
    let valleys = points |> List.filter (fun p -> p.Type = Valley)
    
    peaks.Length |> should equal 34
    valleys.Length |> should equal 28
    
    peaks.[0].Gradient.DataPoint.DateStr |> should startWith "2020-12-22"
    valleys.[0].Gradient.DataPoint.DateStr |> should startWith "2020-12-02"

// Tests for trend analysis
[<Fact>]
let ``analyzeTrend correctly identifies uptrend`` () =
    let points = calculateInflectionPoints uptrendPrices
    let analysis = analyzeTrend points
    
    analysis.Trend |> should equal Uptrend
    analysis.Confidence |> should be (greaterThan 0.5)
    analysis.Confidence |> should be (lessThanOrEqualTo 1.0)

[<Fact>]
let ``analyzeTrend correctly identifies downtrend`` () =
    let points = calculateInflectionPoints downtrendPrices
    let analysis = analyzeTrend points
    
    analysis.Trend |> should equal Downtrend
    analysis.Confidence |> should be (greaterThan 0.5)
    analysis.Confidence |> should be (lessThanOrEqualTo 1.0)

[<Fact>]
let ``analyzeTrend returns InsufficientData for too few points`` () =
    let tooFewBars = uptrendPrices |> Array.take 2
    
    let points = calculateInflectionPoints tooFewBars
    let analysis = analyzeTrend points
    
    analysis.Trend |> should equal InsufficientData
    analysis.Confidence |> should be (lessThan 0.1)

// Tests for trend change detection
[<Fact>]
let ``detectPotentialTrendChange identifies bullish trend change`` () =
    let points = calculateInflectionPoints downtrendPrices
    let latestBar = downtrendPrices[downtrendPrices.Length - 1]
    // create a fake bar with a higher close price
    let fakeBar = new PriceBar(latestBar.Date, latestBar.Open, latestBar.High, latestBar.Low, latestBar.Close + latestBar.Close * 0.1m, latestBar.Volume)    
    let changeAlert = detectPotentialTrendChange points fakeBar
    
    changeAlert.Detected |> should be True
    changeAlert.Direction |> should equal Uptrend
    changeAlert.Strength |> should be (greaterThan 0.4)
    changeAlert.Strength |> should be (lessThanOrEqualTo 1.0)
    changeAlert.Evidence |> should not' (be Empty)

[<Fact>]
let ``detectPotentialTrendChange identifies bearish trend change`` () =
    let points = calculateInflectionPoints downtrendPrices
    let latestBar = downtrendPrices[downtrendPrices.Length - 1]
    // create a fake drop to trigger a bearish alert
    let fakeBar = new PriceBar(latestBar.Date, latestBar.Open, latestBar.High, latestBar.Low, latestBar.Close - latestBar.Close * 0.1m, latestBar.Volume)
    let changeAlert = detectPotentialTrendChange points fakeBar
    
    changeAlert.Detected |> should be True
    changeAlert.Direction |> should equal Downtrend
    changeAlert.Strength |> should be (greaterThan 0.4)
    changeAlert.Strength |> should be (lessThanOrEqualTo 1.0)
    changeAlert.Evidence |> should not' (be Empty)

[<Fact>]
let ``detectPotentialTrendChange handles insufficient data`` () =
    let tooFewBars = downtrendPrices |> Array.take 2
    
    let points = calculateInflectionPoints tooFewBars
    let latestBar = tooFewBars.[tooFewBars.Length - 1]
    let changeAlert = detectPotentialTrendChange points latestBar
    
    changeAlert.Detected |> should be False
    changeAlert.Direction |> should equal InsufficientData
    changeAlert.Evidence |> should contain "Insufficient data"

[<Fact>]
let ``calculateInflectionPoints for downtrend simple average smoothing works``() =

    let inflectionPoints = downtrendPrices |> calculateInflectionPointsByType SimpleAverageSmoothing

    inflectionPoints |> should not' (be Empty)

    let valleys = inflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 31
    
    let peaks = inflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 29

    // last inflection point
    let lastPoint = inflectionPoints.[inflectionPoints.Length - 1]
    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2022-11-22"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 44.97)


[<Fact>]
let ``calculateInflectionPoints for uptrend simple average smoothing works`` () =
    let uptrendInflectionPoints = uptrendPrices |> calculateInflectionPointsByType SimpleAverageSmoothing
    
    uptrendInflectionPoints |> should not' (be Empty)

    let valleys = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 8

    let peaks = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 18

    // last inflection point
    let lastPoint = uptrendInflectionPoints.[uptrendInflectionPoints.Length - 1]

    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2025-02-26"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 65.84m)


[<Fact>]
let ``calculateInflectionPoints for downtrend using volatility smoothing works``() =

    let inflectionPoints = downtrendPrices |> calculateInflectionPointsByType VolatilitySmoothing

    inflectionPoints |> should not' (be Empty)

    let valleys = inflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 32
    
    let peaks = inflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 31

    // last inflection point
    let lastPoint = inflectionPoints.[inflectionPoints.Length - 1]
    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2022-11-22"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 44.97)


[<Fact>]
let ``calculateInflectionPoints for uptrend using volatility smoothing works`` () =
    let uptrendInflectionPoints = uptrendPrices |> calculateInflectionPointsByType VolatilitySmoothing
    
    uptrendInflectionPoints |> should not' (be Empty)

    let valleys = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 12

    let peaks = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 18

    // last inflection point
    let lastPoint = uptrendInflectionPoints.[uptrendInflectionPoints.Length - 1]

    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2025-02-27"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 64.3m)


[<Fact>]
let ``calculateInflectionPoints for downtrend using fixed smoothing works``() =

    let inflectionPoints = downtrendPrices |> calculateInflectionPointsByType FixedSmoothing

    inflectionPoints |> should not' (be Empty)

    let valleys = inflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 28
    
    let peaks = inflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 34

    // last inflection point
    let lastPoint = inflectionPoints.[inflectionPoints.Length - 1]
    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2022-11-22"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 44.97)


[<Fact>]
let ``calculateInflectionPoints for uptrend using fixed smoothing works`` () =
    let uptrendInflectionPoints = uptrendPrices |> calculateInflectionPointsByType FixedSmoothing
    
    uptrendInflectionPoints |> should not' (be Empty)

    let valleys = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Valley)
    valleys |> should haveLength 11

    let peaks = uptrendInflectionPoints |> List.filter (fun p -> p.Type = Peak)
    peaks |> should haveLength 17

    // last inflection point
    let lastPoint = uptrendInflectionPoints.[uptrendInflectionPoints.Length - 1]

    lastPoint.Gradient.DataPoint.DateStr |> should startWith "2025-03-04"
    lastPoint.Type |> should equal Valley
    lastPoint.Gradient.Delta |> should be (greaterThan 0.0)
    lastPoint.Gradient.DataPoint.Close |> should be (equal 61.56m)