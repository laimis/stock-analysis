module studies.BreakoutStudy

open System
open XPlot.Plotly
open core.Shared
open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Services.Analysis.MultipleBarPriceAnalysis
open studies.DataHelpers
open studies.ServiceHelper

// let arr = Array.init 10 (fun i -> i)
// Console.WriteLine("Array:")
// arr |> Array.iter (fun i -> Console.Write("{0}", i))
// Console.WriteLine()

// take 5 from the index of interest
// let index = 9
// let howMany = 5
// let last5 = arr.[(index-howMany+1)..(index)]
// Console.WriteLine("Last 5:")
// last5 |> Array.iter (fun i -> Console.Write("{0}", i))

let private normalizeByAverage (values:float array) =
    let average = values |> Array.average
    values |> Array.map (fun v -> v / average)
    
let private normalizeMinMax values =
    let min = values |> Array.min
    let max = values |> Array.max
    values |> Array.map (fun v -> (v - min) / (max - min))
    
let private normalizeWithinZeroAndMax values =
    let max = values |> Array.max
    values |> Array.map (fun v -> v / max)
    
let calculateBestFitLine normalization (values:float array) =
    let x = [|0.0..(values.Length - 1 |> float)|]
    let y = values |> normalization
    MathNet.Numerics.LinearRegression.SimpleRegression.Fit(x, y)
    
let describe method line = Console.WriteLine("{0}: {1}", method, line)
    
let averageVolumeAndRateAtLastBar (bars:PriceBar array) =
    let stats = DistributionStatistics.calculate (bars[0..(bars.Length-2)] |> Array.map (fun b -> b.Volume |> decimal))
    let volumeRate = bars |> Array.last |> _.Volume |> decimal |> fun x -> x / stats.mean
    (stats.mean, volumeRate)
    
let private calculateAndPrintSlope (prices:PriceBars) (startDate:DateTimeOffset) (endDate:DateTimeOffset) =
    
    let period = 60
        
    let startIndex,startBar = prices.TryFindByDate(startDate) |> Option.get
    let endIndex,endBar = prices.TryFindByDate(endDate) |> Option.get
    let barsForDateRange = prices.Bars[startIndex..endIndex]
    let barsLast60 = prices.Bars[endIndex - period..endIndex]
    
    Console.WriteLine("First bar: {0}", barsForDateRange |> Array.head)
    Console.WriteLine("Last bar: {0}", barsForDateRange |> Array.last)
    Console.WriteLine("Number of bars: {0}", barsForDateRange.Length)
    Console.WriteLine("Number of days: {0}", (endDate - startDate).Days |> int)
    
    // see if pattern detection would trigger
    let breakoutPattern = PatternDetection.contractingVolumeBreakout (barsForDateRange |> PriceBars)
    match breakoutPattern with
    | Some pattern -> Console.WriteLine("Pattern detected: {0}", pattern.description)
    | None -> Console.WriteLine("No pattern detected")
    
    // calculate average volume for those bars
    let averageVolume, volumeRate = averageVolumeAndRateAtLastBar barsForDateRange
    Console.WriteLine()
    Console.WriteLine("Average Volume from {0} to {1}: {2}", startBar.DateStr, endBar.DateStr, averageVolume)
    Console.WriteLine("Volume Rate at breakout: {0}", volumeRate)
    
    let averageVolume60, volumeRate60 = averageVolumeAndRateAtLastBar barsLast60
    Console.WriteLine()
    Console.WriteLine("Average Volume from {0} to {1}: {2}", barsLast60 |> Array.head |> _.DateStr, barsLast60 |> Array.last |> _.DateStr, averageVolume60)
    Console.WriteLine("Volume Rate at breakout: {0}", volumeRate60)
    
    let volumes = barsForDateRange |> Array.map (fun b -> b.Volume |> float)
    
    Console.WriteLine()
    Console.WriteLine("Slope of the volume: {0} to {1}", startBar.DateStr, endBar.DateStr)
    volumes |> calculateBestFitLine normalizeMinMax |> describe "MinMax"
    volumes |> calculateBestFitLine normalizeWithinZeroAndMax |> describe "ZeroMax"
    volumes |> calculateBestFitLine normalizeByAverage |> describe "Average"
    volumes |> calculateBestFitLine id |> describe "None"
    
    let closingPrices = barsForDateRange |> Array.map (fun b -> b.Close |> float)
    
    Console.WriteLine()
    Console.WriteLine("Slope of the closing prices: {0} to {1}", startBar.DateStr, endBar.DateStr)
    closingPrices |> calculateBestFitLine normalizeMinMax |> describe "MinMax"
    closingPrices |> calculateBestFitLine normalizeWithinZeroAndMax |> describe "ZeroMax"
    closingPrices |> calculateBestFitLine normalizeByAverage |> describe "Average"
    closingPrices |> calculateBestFitLine id |> describe "None"
   
type MatchedBreakout = {
    Bars: PriceBar array
    VolumeRate: decimal
    VolumeSlope: float
    CloseSlope: float
}
    with
        member this.Start = this.Bars |> Array.head
        member this.End = this.Bars |> Array.last
    
let plotData (bars: PriceBars) =
    
    let barsOfInterest = bars.Bars
    
    let closes = barsOfInterest |> Array.map (fun bar -> bar.Close)
    let volumes = barsOfInterest |> Array.map (fun bar -> decimal bar.Volume)
    
    let struct(closeIntercept, closeSlope) = MathNet.Numerics.Fit.Line([|0..(barsOfInterest.Length - 2)|] |> Array.map float, closes |> Array.take (barsOfInterest.Length - 1) |> Array.map float)

    let x = [|0..(barsOfInterest.Length - 1)|]
    
    let obv = Indicators.onBalanceVolume bars

    let priceBarTrace =
        XPlot.Plotly.Candlestick(
            x = x,
            ``open`` = (barsOfInterest |> Array.map (fun bar -> bar.Open)),
            high = (barsOfInterest |> Array.map (fun bar -> bar.High)),
            low = (barsOfInterest |> Array.map (fun bar -> bar.Low)),
            close = (barsOfInterest |> Array.map (fun bar -> bar.Close)),
            showlegend = false
        ) :> Trace
        
    let closeTrace = 
        XPlot.Plotly.Scatter(
            x = x,
            y = (closes |> Array.map float),
            mode = "lines",
            name = "Close"
        ) :> Trace
        
    let obvTrace = 
        XPlot.Plotly.Scatter(
            x = x,
            y = (obv |> Array.map (fun d -> d.Value |> float)),
            mode = "lines",
            name = "OBV",
            showlegend = false,
            yaxis = "y2"
        ) :> Trace
        
    let closeSlopeTrace = 
        XPlot.Plotly.Scatter(
            x = x,
            y = (x |> Array.map (fun x -> closeIntercept + closeSlope * float x)),
            mode = "lines",
            name = "Close Slope",
            showlegend = false
        ) :> Trace

    let volumeTrace = 
        Bar(
            x = x,
            y = (volumes |> Array.map float),
            name = "Volume",
            showlegend = false
        ) :> Trace
    
    let struct (volumeIntercept, volumeSlope) = MathNet.Numerics.Fit.Line(x |> Array.take (x.Length - 1) |> Array.map float, volumes |> Array.take (volumes.Length - 1) |> Array.map float)
    
    let volumeSlopeTrace = 
        XPlot.Plotly.Scatter(
            x = x,
            y = (x |> Array.map (fun x -> volumeIntercept + volumeSlope * float x)),
            mode = "lines",
            name = "Volume Slope",
            showlegend = false
        )

    let priceLayout =
        Layout(
            title = "Price",
            xaxis = Xaxis(title = "Bar Index"),
            yaxis = Yaxis(title = "Price"),
            yaxis2 = Yaxis(title = "OBV", overlaying = "y", side = "right")
        )

    let volumeLayout =
        Layout(
            title = "Volume",
            xaxis = Xaxis(title = "Bar Index"),
            yaxis = Yaxis(title = "Volume")
        )

    let priceChart = 
        [ priceBarTrace; closeSlopeTrace; obvTrace ]
        |> Chart.Plot
        |> Chart.WithLayout priceLayout

    let volumeChart =
        [ volumeTrace; volumeSlopeTrace ]
        |> Chart.Plot
        |> Chart.WithLayout volumeLayout

    // let combinedChart =
    [ priceChart; volumeChart ]
        |> Chart.ShowAll // Arrange the charts in a 1x2 grid (one row, two columns)

    // combinedChart |> Chart.Show
    
let private contractingVolumeBreakout (bars: PriceBars) =
    
    let contractingVolumeBreakoutName = "Contracting Volume Breakout"
    let volumeRateThreshold = 1.3m
    let volumeSlopeThreshold = 0.0
    let rangeSlopeThreshold = 0.0
    
    let barsOfInterest = bars.Bars[0..bars.Length-2]
        
    let volumes = barsOfInterest |> Array.map (fun bar -> decimal bar.Volume)
    let ranges = barsOfInterest |> Array.map (fun bar -> bar.High - bar.Low)
    
    let volumeStats = volumes |> DistributionStatistics.calculate
    let rangeStats = ranges |> DistributionStatistics.calculate
    
    let lastBar = bars.Last
    let lastVolume = decimal lastBar.Volume
    let lastVolumeRate = lastVolume / volumeStats.mean
    let lastRange = ranges |> Seq.last
    
    let x = [|0.0..(volumes.Length - 1 |> float)|]
    let volY = volumes |> Array.map float
    let struct (_, volSlope) = MathNet.Numerics.Fit.Line(x, volY)
    
    let rangeY = ranges |> Seq.map float |> Seq.toArray
    let struct (_, rangeSlope) = MathNet.Numerics.Fit.Line(x, rangeY)
    
    let struct (_, closeSlope) = MathNet.Numerics.Fit.Line(x, barsOfInterest |> Array.map (fun bar -> bar.Close |> float))
   
    let description = $"{contractingVolumeBreakoutName}: {lastVolumeRate:N1}x, Vol Slope: {volSlope:N2}, Price Slope: {closeSlope:N2}"
        
    if lastVolumeRate > volumeRateThreshold && 
       volSlope < volumeSlopeThreshold &&
       lastBar.Close > lastBar.Open
       // && rangeSlope < rangeSlopeThreshold
       then

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
            
let private runInternal (context:EnvironmentContext) userState = async {
    
    let debugBars bars =
        Console.WriteLine("Bars: {0}, {1}, {2}", bars |> Array.head, bars |> Array.last, bars.Length)
        
    let debug (matched:MatchedBreakout) =
        Console.WriteLine($"Start: {matched.Start.DateStr}, End: {matched.End.DateStr}, Rate: {matched.VolumeRate:N2}x, VolumeSlope: {matched.VolumeSlope:N2}, AtrSlope: {matched.CloseSlope:N2}")
        
    Console.Write("Ticker: ")
    let tickerSymbol = Console.ReadLine()
    let ticker = Ticker(tickerSymbol)
    
    let! priceBars = context.Brokerage().GetPriceHistory userState ticker PriceFrequency.Daily None None |> Async.AwaitTask
    match priceBars with
    | Error err -> failwith err.Message
    | Ok bars ->
            
        let atr = Indicators.averageTrueRage bars |> _.DataPoints
        
        bars.Bars
        |> Array.windowed 41
        |> Array.iter (
            fun window ->
                let breakout = contractingVolumeBreakout (window |> PriceBars)
                match breakout with
                | Some pattern ->
                    Console.WriteLine(window[window.Length-1].DateStr + ": " + pattern.description)
                    plotData (window |> PriceBars)
                    Console.ReadLine() |> ignore
                | None -> ()
        )
            
}   

let run (context:EnvironmentContext) = async {  
    let! user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask
    match user with
    | None -> failwith "User not found"
    | Some user -> return! runInternal context user.State
}
