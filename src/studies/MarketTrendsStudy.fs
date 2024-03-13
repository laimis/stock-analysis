module studies.MarketTrendsStudy

open System
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Services.Trends
open FSharp.Data
open studies.ServiceHelper

      
// csv type provider to export trend
type TrendCsv = CsvProvider<Sample="Ticker,Direction,Start,End,Bars,StartValue,EndValue,GainPercent,MaxDate,MaxValue,MinDate,MinValue,TrendType", HasHeaders=true>

let private outputToCsv (outputFilename:string) (trends:Trends) =
    // output to csv
    let rows =
        trends.Trends
        |> List.map (fun t ->
            TrendCsv.Row(
                ticker=t.ticker.Value,
                direction=(match t.direction with | Up -> "Up" | Down -> "Down"),
                start=(t.start |> snd |> _.DateStr),
                ``end``=(t.end_ |> snd |> _.DateStr),
                bars=t.NumberOfBars.ToString(),
                startValue=t.StartValue.ToString(),
                endValue=t.EndValue.ToString(),
                gainPercent=t.GainPercent.ToString("P"),
                maxDate=t.MaxDate,
                maxValue=t.MaxValue.ToString(),
                minDate=t.MinDate,
                minValue=t.MinValue.ToString(),
                trendType=(match trends.TrendType with | Ema20OverSma50 -> "Ema20OverSma50" | Sma50OverSma200 -> "Sma50OverSma200")
            )
        )
        
    let csv = new TrendCsv(rows)
    
    csv.Save(outputFilename)
    
let private outputToConsole (trends:Trends) =
    // describe the trends
    Console.WriteLine($"Trends for {trends.Ticker} from {trends.StartDateStr} to {trends.EndDateStr}")
    Console.WriteLine($"Trend Type: {trends.TrendType}")
    Console.WriteLine($"Number of trends: {trends.Length}")
    
    let describeStatistics stats =
        Console.WriteLine($"Average number of bars: {stats.mean}")
        Console.WriteLine($"Median number of bars: {stats.median}")
        Console.WriteLine($"Maximum number of bars: {stats.max}")
        Console.WriteLine($"Minimum number of bars: {stats.min}")
            
    Console.WriteLine("All trends")
    trends.BarStatistics |> describeStatistics
    
    // describe down trends
    Console.WriteLine("\nUp trends")
    trends.UpBarStatistics |> describeStatistics
    
    // describe down trends
    Console.WriteLine("\nDown trends")
    trends.DownBarStatistics |> describeStatistics
    
    // take the last trend which is the current trend
    let currentTrend = trends.CurrentTrend
    
    Console.WriteLine("\nCurrent trend: " + (match currentTrend.direction with | Up -> "Up" | Down -> "Down") + " trend")
    
    Console.WriteLine($"Start: {currentTrend.start |> snd |> _.DateStr}")
    Console.WriteLine($"End: {currentTrend.end_ |> snd |> _.DateStr}")
    Console.WriteLine($"Bars: {currentTrend.NumberOfBars}")
    Console.WriteLine($"Start Value: {currentTrend.StartValue}")
    Console.WriteLine($"End Value: {currentTrend.EndValue}")
    Console.WriteLine($"Gain Percent: {currentTrend.GainPercent:P}")
    Console.WriteLine($"Max Date: {currentTrend.MaxDate}, age in bars: {currentTrend.MaxAge}")
    Console.WriteLine($"Max Value: {currentTrend.MaxValue}")
    Console.WriteLine($"Min Date: {currentTrend.MinDate}, age in bars: {currentTrend.MinAge}")
    Console.WriteLine($"Min Value: {currentTrend.MinValue}")
    
    // let's rank the current trend in the context of all trends that match its direction
    let rankByNumberOfBars,trendTotal = trends.BarRank currentTrend
    let rankByGain,_ = trends.GainRank currentTrend
    
    Console.WriteLine($"Rank by number of bars: {rankByNumberOfBars} out of {trendTotal}")
    Console.WriteLine($"Rank by gain percent: {rankByGain} out of {trendTotal}")
    
let run (context:EnvironmentContext) =
    
    // get the user
    let user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()
    
    let ticker = context.GetArgumentValue "-t" |> Ticker
    let years = context.GetArgumentValue "-y" |> int
    let outputFilename = context.GetArgumentValue "-o"
    let trendType = context.GetArgumentValue "-tt" |> TrendType.FromString
    
    // confirm the input
    Console.WriteLine($"Ticker: {ticker}, Years: {years}, Output: {outputFilename}")
    
    // get the brokerage
    let brokerage = context.Brokerage()
    
    // get prices for ticker for the specified number of years
    let prices =
        brokerage.GetPriceHistory user.Value.State ticker PriceFrequency.Daily (DateTimeOffset.Now.AddYears(-years) |> Some) None
        |> Async.AwaitTask
        |> Async.RunSynchronously
        
    match prices with
    | Error e -> failwith e.Message
    | Ok prices ->
        
        let trends = prices |> TrendCalculator.generate ticker trendType
        
        trends |> outputToCsv outputFilename
        
        trends |> outputToConsole
        
        
