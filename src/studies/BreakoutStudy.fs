module studies.BreakoutStudy

open System
open XPlot.Plotly
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Services.Analysis.MultipleBarPriceAnalysis
open studies.DataHelpers
open studies.ServiceHelper
open FSharp.Data

// let arr = Array.init 10 (fun i -> i)
// Console.WriteLine("Array:")
// arr |> Array.iter (fun i -> Console.Write("{0}", i))
// Console.WriteLine()

// take 5 from the index of interest
// let index = 9
// let howMany = 5
// let last5 = arr[(index-howMany+1)..(index)]
// Console.WriteLine("Last 5:")
// last5 |> Array.iter (fun i -> Console.Write("{0}", i))

type BreakoutSignalRecord =
    CsvProvider<
        Schema = "date (string), ticker (string), breakoutVolumeRate (decimal), volumeSlope (decimal), priceSlope (decimal)",
        HasHeaders=false
    >
    
type BreakoutSignalRecordWrapper(row:BreakoutSignalRecord.Row) =
    interface ISignal with
        member this.Date = row.Date
        member this.Ticker = row.Ticker
        member this.Gap = None
        member this.Screenerid = None
        member this.Sma20 = None
        member this.Sma50 = None
        member this.Sma150 = None
        member this.Sma200 = None
        
        

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
    Ticker: string
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
        Candlestick(
            x = x,
            ``open`` = (barsOfInterest |> Array.map (fun bar -> bar.Open)),
            high = (barsOfInterest |> Array.map (fun bar -> bar.High)),
            low = (barsOfInterest |> Array.map (fun bar -> bar.Low)),
            close = (barsOfInterest |> Array.map (fun bar -> bar.Close)),
            showlegend = false
        ) :> Trace
        
    let closeTrace = 
        Scatter(
            x = x,
            y = (closes |> Array.map float),
            mode = "lines",
            name = "Close"
        ) :> Trace
        
    let obvTrace = 
        Scatter(
            x = x,
            y = (obv |> Array.map (fun d -> d.Value |> float)),
            mode = "lines",
            name = "OBV",
            showlegend = false,
            yaxis = "y2"
        ) :> Trace
        
    let closeSlopeTrace = 
        Scatter(
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
        Scatter(
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
    
let private contractingVolumeBreakout ticker (bars: PriceBars) =
    
    let contractingVolumeBreakoutName = "Contracting Volume Breakout"
    let volumeRateThreshold = 1.3m
    let volumeSlopeThreshold = 0.0
    
    let barsOfInterest = bars.Bars[0..bars.Length-2]
        
    let volumes = barsOfInterest |> Array.map (fun bar -> decimal bar.Volume)
    
    let volumeStats = volumes |> DistributionStatistics.calculate
    
    let lastBar = bars.Last
    let lastVolume = decimal lastBar.Volume
    let lastVolumeRate = lastVolume / volumeStats.mean
    
    let x = [|0.0..(volumes.Length - 1 |> float)|]
    let volY = volumes |> Array.map float
    let struct (_, volSlope) = MathNet.Numerics.Fit.Line(x, volY)
    
    
    let struct (_, closeSlope) = MathNet.Numerics.Fit.Line(x, barsOfInterest |> Array.map (fun bar -> bar.Close |> float))
   
    if lastVolumeRate > volumeRateThreshold && 
       volSlope < volumeSlopeThreshold &&
       lastBar.Close > lastBar.Open
       then

        Some({
            Bars = bars.Bars
            VolumeRate = lastVolumeRate
            VolumeSlope = volSlope
            CloseSlope = closeSlope
            Ticker = ticker
        })
    else
        None
            
let private runInteractive (context:EnvironmentContext) userState = async {
    
    let toString (matched:MatchedBreakout) =
        $"DAte: {matched.End.DateStr}, VolumeRate: {matched.VolumeRate:N2}x, VolumeSlope: {matched.VolumeSlope:N2}, PriceSlope: {matched.CloseSlope:N2}"
    
    Console.Write("Ticker: ")
    let tickerSymbol = Console.ReadLine()
    let ticker = Ticker(tickerSymbol)
    
    let! priceBars = context.Brokerage().GetPriceHistory userState ticker PriceFrequency.Daily None None |> Async.AwaitTask
    match priceBars with
    | Error err -> failwith err.Message
    | Ok bars ->
            
        let matches =
            bars.Bars
            |> Array.windowed 41
            |> Array.map (fun window ->
                match contractingVolumeBreakout tickerSymbol (window |> PriceBars) with
                | Some pattern -> Some (window, pattern)
                | None -> None)
            |> Array.choose id
            
        // print all matches with index
        matches |> Array.iteri (fun i (_,pattern) -> Console.WriteLine("{0}: {1}", i, toString pattern))
        
        Console.Write("Which one to plot:")
        let index = Console.ReadLine() |> int
        
        let bars,_ = matches[index]
        
        plotData (bars |> PriceBars)
}

let getPricesForTickerOption studiesDirectory ticker = async {
    let! priceAvailability = tryGetPricesFromCsv studiesDirectory ticker
    match priceAvailability with
    | Ok prices ->
        return Some (ticker, prices)
    | Error _ ->
        return None
}

let getBreakoutPatternsForTickerIfAvailable tickerSymbol (bars:PriceBars) =
        bars.Bars
        |> Array.windowed 41
        |> Array.map (fun window ->
            match contractingVolumeBreakout tickerSymbol (window |> PriceBars) with
            | Some pattern -> Some (window, pattern)
            | None -> None)
        |> Array.choose id

let runStudy (context:EnvironmentContext) _ = async {
    
    let studiesDirectory = context.GetArgumentValue "-d"
    
    let tickersWithPriceAvailable = studiesDirectory |> getTickersWithPriceHistory
            
    // for each of these tickers, try to obtain price history
    let! tryPriceHistory =
        tickersWithPriceAvailable
        |> Array.map(getPricesForTickerOption studiesDirectory) 
        |> Async.Parallel
        
    let priceHistory = tryPriceHistory |> Array.choose id
    
    Console.WriteLine($"Tickers with price history: {priceHistory.Length}")
    
    // filter out tickers that don't have expected most recent price data
    let mostRecentDate = "2024-05-21"
    let priceHistory =
        priceHistory
        |> Array.filter (fun (_, prices) -> prices.Bars |> Array.last |> _.DateStr = mostRecentDate)
        
    Console.WriteLine($"Tickers with most recent ({mostRecentDate}) price data: {priceHistory.Length}")
    
    let matchingPatterns =
        priceHistory
        |> Array.map (fun (ticker, prices) -> getBreakoutPatternsForTickerIfAvailable ticker prices)
        |> Array.concat
    
    Console.WriteLine($"Matching patterns: {matchingPatterns.Length}")
        
    // prune the list so that the signals that are within two weeks of each other are removed
    let signals =
        matchingPatterns
        |> Array.groupBy (fun (_,pattern) -> pattern.Ticker)
        |> Array.collect(fun (_,tickerSignals) ->
            
            tickerSignals
            |> Array.map snd
            |> Array.fold (fun (acc:MatchedBreakout array) signal ->
                match acc with
                | [||] -> [|signal|]
                | _ ->
                    let previousSignal = acc |> Array.head
                    match signal.End.Date - previousSignal.End.Date with
                    | x when x.TotalDays > 14 -> Array.append [|signal|] acc
                    | _ -> acc
            ) [||]
            |> Array.rev
        )
    
    Console.WriteLine($"Signals: {signals.Length}")
    
    // now, get rid of signals where I don't have price data
    let! withPrices =
        signals
        |> Array.map( fun breakout -> async { 
            let! prices = getPricesFromCsv studiesDirectory breakout.Ticker
            
            // get the bar at the breakout day
            let barWithIndex = prices.TryFindByDate breakout.End.Date
            match barWithIndex with
            | Some (index, _) ->
                let nextDayBar = prices.Bars |> Array.tryItem (index + 1)
                match nextDayBar with
                | Some _ -> return Some breakout
                | None -> return None
            | None -> return None
        }
        )
        |> Async.Sequential
        
    let signals =
        withPrices
        |> Array.choose id
        |> Array.map( fun breakout -> 
                BreakoutSignalRecord.Row(
                    date = breakout.End.DateStr,
                    ticker = breakout.Ticker,
                    breakoutVolumeRate = breakout.VolumeRate,
                    volumeSlope = decimal breakout.VolumeSlope,
                    priceSlope = decimal breakout.CloseSlope
                )
            )
        
    Console.WriteLine($"Signals with prices: {signals.Length}")
    
    let outputPath = context.GetArgumentValue "-o"
    
    let csv = new BreakoutSignalRecord(signals)
    
    csv.Save(outputPath)
}

let run (context:EnvironmentContext) = async {  
    let! user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask
    match user with
    | None -> failwith "User not found"
    | Some user ->
        
        let funcToRun =
            match context.HasArgument "--interactive" with
            | true -> runInteractive
            | false -> runStudy
                
        
        return! funcToRun context user.State
}
