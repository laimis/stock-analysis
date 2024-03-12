module studies.MarketTrendsStudy

open System
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open FSharp.Data

type TrendDirection =
    | Up
    | Down
    
type TrendType =
    | Ema20OverSma50
    | Sma50OverSma200
    
    static member parse s =
        match s with
        | nameof Ema20OverSma50 -> Ema20OverSma50
        | nameof Sma50OverSma200 -> Sma50OverSma200
        | _ -> failwith $"Invalid trend type {s}"
    
type Trend = {
    ticker: Ticker
    start: PriceBarWithIndex
    end_: PriceBarWithIndex
    max: PriceBarWithIndex
    min: PriceBarWithIndex
    direction: TrendDirection
    trendType: TrendType
}
    with
        member this.NumberOfDays =
            let end_ = this.end_ |> snd
            let start = this.start |> snd
            (end_.Date - start.Date).TotalDays |> int
        member this.NumberOfBars =
            let end_ = this.end_ |> fst
            let start = this.start |> fst
            end_ - start
            
        member this.StartValue = this.start |> snd |> _.Close
        member this.EndValue = this.end_ |> snd |> _.Close
        member this.GainPercent = (this.EndValue - this.StartValue) / this.StartValue
        member this.MaxDate = this.max |> snd |> _.DateStr
        member this.MaxValue = this.max |> snd |> _.Close
        member this.MaxAge = (this.end_ |> fst) - (this.max |> fst)
        member this.MinDate = this.min |> snd |> _.DateStr
        member this.MinValue = this.min |> snd |> _.Close
        member this.MinAge = (this.end_ |> fst) - (this.min |> fst)
        
type Trends(ticker, trendType, trends:Trend list) =
    member this.Trends = trends
    member this.UpTrends = trends |> List.filter (fun t -> t.direction = Up)
    member this.DownTrends = trends |> List.filter (fun t -> t.direction = Down)
    member this.CurrentTrend = trends |> List.last
    member this.Length = trends.Length
    member this.BarStatistics = trends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.UpBarStatistics = this.UpTrends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.DownBarStatistics = this.DownTrends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.GainStatistics = trends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.UpGainStatistics = this.UpTrends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.DownGainStatistics = this.DownTrends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.TrendType = trendType
    member this.Ticker = ticker
    member this.StartDateStr = trends |> List.head |> fun t -> t.start |> snd |> _.DateStr
    member this.EndDateStr = this.CurrentTrend.end_ |> snd |> _.DateStr
    
module TrendCalculator =
    
    let private trendMASelection trendType (movingAverages:MovingAveragesContainer) =
        match trendType with
        | Ema20OverSma50 -> movingAverages.ema20, movingAverages.sma50
        | Sma50OverSma200 -> movingAverages.sma50, movingAverages.sma200
    
    let generate ticker trendType (prices:PriceBars) =
        
        let firstMas,secondMas = prices |> MovingAveragesContainer.Generate |> trendMASelection trendType
        
        let indexedValues =
            Array.mapi2 (fun i firstMa secondMa -> i, firstMa, secondMa) firstMas.Values secondMas.Values
            |> Array.filter (fun (_,firstMa,secondMa) -> firstMa.IsSome && secondMa.IsSome)
            |> Array.map (fun (i,firstMa,secondMa) -> i, firstMa.Value, secondMa.Value)
            
        let createTrend foundLocation direction trendType =
            let barWithIndex = prices.BarsWithIndex[foundLocation]
            
            {
                ticker = ticker
                start = barWithIndex
                end_ = barWithIndex
                max = barWithIndex
                min = barWithIndex
                direction = direction
                trendType = trendType
            }
            
        let initialDirection =
            match indexedValues[0] with
            | _, first, second ->
                match first > second with
                | true -> Up
                | false -> Down
                
        let initialTrend = createTrend 0 initialDirection trendType
            
        let latestTrendAndTrends =
            indexedValues
            |> Array.fold (fun (trend, trends) (i, firstMa, secondMa) ->
                
                let direction =
                    match firstMa > secondMa with
                    | true -> Up
                    | false -> Down
                
                match direction = trend.direction with
                | true ->
                    // check the max and see it needs to be updated
                    let max =
                        match prices.BarsWithIndex[i] with
                        | currentIndexWithBar when (currentIndexWithBar |> snd).Close > (trend.max |> snd).Close -> currentIndexWithBar
                        | _ -> trend.max
                        
                    // check the min and see it needs to be updated
                    let min =
                        match prices.BarsWithIndex[i] with
                        | currentIndexWithBar when (currentIndexWithBar |> snd).Close < (trend.min |> snd).Close -> currentIndexWithBar
                        | _ -> trend.min
                    
                    let newTrend = { trend with end_ = prices.BarsWithIndex[i]; max = max; min = min }
                    newTrend, trends
                | false ->
                    let newTrends = trends @ [trend]
                    let newTrend = createTrend i direction trendType
                    newTrend, newTrends
            ) (initialTrend, [])
            
        let trend, trends = latestTrendAndTrends
        
        // finish the last trend and add it to trends
        let trend = { trend with end_ = prices.BarsWithIndex[prices.BarsWithIndex.Length - 1] }
        
        Trends(ticker, trendType, trends @ [trend])
        
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
    let matchingTrends = 
        match currentTrend.direction with
        | Up -> trends.UpTrends
        | Down -> trends.DownTrends
        
    let compareByNumberOfBars = fun (t:Trend) -> t.NumberOfBars
    let compareByGain = fun (t:Trend) -> t.GainPercent
    
    let rankByNumberOfBars = matchingTrends |> List.sortByDescending compareByNumberOfBars |> List.findIndex (fun t -> t = currentTrend) |> fun x -> x + 1
    let rankByGain = matchingTrends |> List.sortByDescending compareByGain |> List.findIndex (fun t -> t = currentTrend) |> fun x -> x + 1
    
    Console.WriteLine($"Rank by number of bars: {rankByNumberOfBars} out of {matchingTrends.Length}")
    Console.WriteLine($"Rank by gain percent: {rankByGain} out of {matchingTrends.Length}")
    
let run() =
    
    // get the user
    let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()
    
    let ticker = ServiceHelper.getArgumentValue "-t" |> Ticker
    let years = ServiceHelper.getArgumentValue "-y" |> int
    let outputFilename = ServiceHelper.getArgumentValue "-o"
    let trendType = ServiceHelper.getArgumentValue "-tt" |> TrendType.parse
    
    // confirm the input
    Console.WriteLine($"Ticker: {ticker}, Years: {years}, Output: {outputFilename}")
    
    // get the brokerage
    let brokerage = ServiceHelper.brokerage()
    
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
        
        
