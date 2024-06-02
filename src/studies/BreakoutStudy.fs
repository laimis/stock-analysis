module studies.BreakoutStudy

open System
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
    Atr: DataPoint<decimal> array
    VolumeRate: decimal
    VolumeSlope: float
    AtrSlope: float
}
    with
        member this.Start = this.Bars |> Array.head
        member this.End = this.Bars |> Array.last
        
let private filterToCertainGapsOnly (context:EnvironmentContext) userState = async {
    
    let debugBars bars =
        Console.WriteLine("Bars: {0}, {1}, {2}", bars |> Array.head, bars |> Array.last, bars.Length)
        
    let debug (matched:MatchedBreakout) =
        Console.WriteLine($"Start: {matched.Start.DateStr}, End: {matched.End.DateStr}, Rate: {matched.VolumeRate:N2}x, VolumeSlope: {matched.VolumeSlope:N2}, AtrSlope: {matched.AtrSlope:N2}")
        
    Console.Write("Ticker: ")
    let tickerSymbol = Console.ReadLine()
    let ticker = Ticker(tickerSymbol)
    
    let! priceBars = context.Brokerage().GetPriceHistory userState ticker PriceFrequency.Daily None None |> Async.AwaitTask
    match priceBars with
    | Error err -> failwith err.Message
    | Ok bars ->
            
        let atr = Indicators.averageTrueRage bars |> _.DataPoints
        
        // window of 40 bars
        // let bars =
        bars.Bars
        |> Array.windowed 41
        |> Array.map (
            fun window ->
                
                let preceding = window[0..39]
                let volX = [|0.0..(preceding.Length - 1 |> float)|]
                let volY = preceding |> Array.map (fun b -> b.Volume |> float)
                let struct (_, volumeSlope) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(volX, volY)
                let negativeVolumeSlope = volumeSlope < 0.0
                
                let _, rate = averageVolumeAndRateAtLastBar window
                let highBreakout = rate > 1.3m
                
                let lastBar = window |> Array.last
                let closedPositive = lastBar.Close > lastBar.Open
                
                // find the atr bar for the date of the last bar
                let atrEndIndex = atr |> Array.findIndex (fun b -> b.Label = lastBar.DateStr)
                let atrStartIndex =
                    match atrEndIndex - window.Length - 1 with
                    | x when x < 0 -> 0
                    | x -> x
                    
                let atrWindow = atr[atrStartIndex+2..atrEndIndex]
                let atrX = [|0.0..(atrWindow.Length - 1 |> float)|]
                let atrY = atrWindow |> Array.map (fun b -> b.Value |> float)
                let struct (_, atrSlope) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(atrX, atrY)
                let negativeAtrSlope = atrSlope < 0.0
                
                match negativeVolumeSlope && highBreakout && closedPositive && negativeAtrSlope with
                | true ->
                    Some {
                        Bars = window
                        Atr = atrWindow
                        AtrSlope = atrSlope
                        VolumeRate = rate
                        VolumeSlope = volumeSlope 
                    }
                | false -> None
        )
        |> Array.choose id
        |> Array.iter(fun matched ->
            debug matched
            // Console.ReadLine() |> ignore
        )
            
}   

let run (context:EnvironmentContext) = async {  
    let! user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask
    match user with
    | None -> failwith "User not found"
    | Some user -> return! filterToCertainGapsOnly context user.State
}
