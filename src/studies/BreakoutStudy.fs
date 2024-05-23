module studies.BreakoutStudy

open System
open core.Shared
open core.fs.Adapters.Stocks
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
    
let calculateBestFitLine normalization (values:float array) =
    let x = [|0.0..(values.Length - 1 |> float)|]
    let y = values |> normalization
    MathNet.Numerics.Fit.Line(x, y)
    
let describe method line = Console.WriteLine("{0}: {1}", method, line)
    
let private calculateAndPrintSlope (prices:PriceBars) (startDate:DateTimeOffset) (endDate:DateTimeOffset) period =
    
    // get april 16th, 2024 bar
    let startIndex,startBar = prices.TryFindByDate(startDate) |> Option.get
    let endIndex,endBar = prices.TryFindByDate(endDate) |> Option.get
    let last60InclusiveTheIndex = prices.Bars[startIndex..endIndex]
    
    Console.WriteLine("First bar: {0}", last60InclusiveTheIndex |> Array.head)
    Console.WriteLine("Last bar: {0}", last60InclusiveTheIndex |> Array.last)
    Console.WriteLine("Number of bars: {0}", last60InclusiveTheIndex.Length)
    Console.WriteLine("Number of days: {0}", (endDate - startDate).Days |> int)
    
    // calculate average volume for those bars
    let totalVolume = last60InclusiveTheIndex |> Seq.map (_.Volume) |> Seq.sum |> decimal
    let averageVolume = totalVolume / decimal period
    let volumeRate = decimal endBar.Volume / averageVolume
    Console.WriteLine("Average Volume: {0}", averageVolume)
    Console.WriteLine("Volume Rate at breakout: {0}", volumeRate)
    
    let volumes = last60InclusiveTheIndex |> Array.map (fun b -> b.Volume |> float)
    
    Console.WriteLine("Slope of the volume:")
    volumes |> calculateBestFitLine normalizeMinMax |> describe "MinMax"
    volumes |> calculateBestFitLine normalizeByAverage |> describe "Average"
    volumes |> calculateBestFitLine id |> describe "None"
    
    let closingPrices = last60InclusiveTheIndex |> Array.map (fun b -> b.Close |> float)
    
    Console.WriteLine("Slope of the closing prices:")
    closingPrices |> calculateBestFitLine normalizeMinMax |> describe "MinMax"
    closingPrices |> calculateBestFitLine normalizeByAverage |> describe "Average"
    closingPrices |> calculateBestFitLine id |> describe "None"
    
let private filterToCertainGapsOnly (context:EnvironmentContext) userState = async {
    
    let! prices = getPricesFromCsv (context.GetArgumentValue "-d") (Ticker("SE"))
    // let start = DateTimeOffset(2024, 3, 4, 0, 0, 0, TimeSpan.Zero)
    // let end' = DateTimeOffset(2024, 4, 16, 0, 0, 0, TimeSpan.Zero)
    // from jan 24th to march 4 of 2024
    let start = DateTimeOffset(2024, 1, 24, 0, 0, 0, TimeSpan.Zero)
    let end' = DateTimeOffset(2024, 3, 4, 0, 0, 0, TimeSpan.Zero)
    let period = 60
    
    calculateAndPrintSlope prices start end' period
}   

let run (context:EnvironmentContext) = async {  
    let! user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask
    match user with
    | None -> failwith "User not found"
    | Some user -> return! filterToCertainGapsOnly context user.State
}
