module studies.MarketTrendsStudy

open System
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open FSharp.Data

type TrendDirection =
    | Up
    | Down
    
type Trend = {
    ticker: Ticker
    start: PriceBar
    startIndex: int
    end_: PriceBar
    endIndex: int
    direction: TrendDirection
}
    with
        member this.NumberOfDays = (this.end_.Date - this.start.Date).TotalDays |> int
        member this.NumberOfBars = this.endIndex - this.startIndex
        
        
// csv type provider to export trend
type TrendCsv = CsvProvider<Sample="Ticker,Direction,Start,End,Days,Bars", HasHeaders=true>
    
let run() =
    
    // get the user
    let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()
    
    let ticker = ServiceHelper.getArgumentValue "-t" |> Ticker
    let years = ServiceHelper.getArgumentValue "-y" |> int
    let outputFilename = ServiceHelper.getArgumentValue "-o"
    
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
        
        // calculate 20 and 50 day moving averages
        let movingAverages = MovingAveragesContainer.Generate prices
        
        let indexedEma20AndSma50 =
            Array.mapi2 (fun i ema20Val sma50val -> i, ema20Val, sma50val) movingAverages.ema20.Values movingAverages.sma50.Values
            |> Array.filter (fun (_,ema20,sma50) -> ema20.IsSome && sma50.IsSome)
            |> Array.map (fun (i,ema20,sma50) -> i, ema20.Value, sma50.Value)
            
        let createTrend foundLocation direction =
            let index,bar = prices.BarsWithIndex[foundLocation]
            
            {
                ticker = ticker
                start = bar
                startIndex = index
                end_ = bar
                endIndex = index
                direction = direction
            }
            
        let initialDirection =
            match indexedEma20AndSma50[0] with
            | _, ema, sma ->
                match ema > sma with
                | true -> Up
                | false -> Down
                
        let initialTrend = createTrend 0 initialDirection
            
        let latestTrendAndTrends =
            indexedEma20AndSma50
            |> Array.fold (fun (trend, trends) (i, ema20, sma50) ->
                
                let direction =
                    match ema20 > sma50 with
                    | true -> Up
                    | false -> Down
                
                match direction = trend.direction with
                | true ->
                    let newTrend = { trend with end_ = prices.BarsWithIndex[i] |> snd; endIndex = prices.BarsWithIndex[i] |> fst }
                    newTrend, trends
                | false ->
                    let newTrends = trends @ [trend]
                    let newTrend = createTrend i direction
                    newTrend, newTrends
            ) (initialTrend, [])
            
        let trend, trends = latestTrendAndTrends
        
        // finish the last trend and add it to trends
        let trend = { trend with end_ = prices.BarsWithIndex.[prices.BarsWithIndex.Length - 1] |> snd; endIndex = prices.BarsWithIndex.Length - 1 }
        let trends = trends @ [trend]
        
        // output to csv
        let rows =
            trends
            |> List.map (fun t -> TrendCsv.Row(ticker=t.ticker.Value, direction=(match t.direction with | Up -> "Up" | Down -> "Down"), start=t.start.DateStr,``end``=t.end_.DateStr, days=t.NumberOfDays.ToString(), bars=t.NumberOfBars.ToString()))
            
        let csv = new TrendCsv(rows)
        
        csv.Save(outputFilename)
            
