module studies.BreakoutStudy

open core.fs.Adapters.Stocks
open studies.DataHelpers
open studies.ScreenerStudy
open studies.ServiceHelper



// TODO: this function is a bit nuts. WIP to refactor
// but using it as is while I feel it out how do I want to
// deal with stuff like VERY specific strategy behavior
let private filterToCertainGapsOnly (context:EnvironmentContext) userState = async {
    let inputFilename = context.GetArgumentValue "-f"
    let outputFilename = context.GetArgumentValue "-o"
    let studiesDirectory = context.GetArgumentValue "-d"
    
    let brokerage = context.Brokerage()    
    let pricesWrapper =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    brokerage.GetPriceHistory userState ticker PriceFrequency.Daily start ``end``
        }
    
    let signals = SignalWithPriceProperties.Load inputFilename |> _.Rows
    let! runFilter =
        signals
        |> Seq.filter (fun r -> r.Gap > 0.01m)
        |> Seq.map (fun r -> async {
                // for each of those, take only those that has a volume that's going down
    
                let! prices = getPricesFromCsv studiesDirectory r.Ticker
                let index = prices.TryFindByDate r.Date |> Option.get
                
                let barData =
                    prices.Bars
                    |> Array.mapi (fun i bar ->
                        let startIndex = max 0 (i - 59)
                        let endIndex = i
                        let volumeWindow = prices.Bars[startIndex..endIndex] |> Array.map (fun b -> decimal b.Volume)
                        let avgVolume = if volumeWindow.Length > 0 then volumeWindow |> Array.average else 0m
                        (bar, avgVolume)
                )
                
                // let's make sure that the volume ratio at the day of the signal is more than 1.3x
                let _,signalAvgVolume = barData[index |> fst]
                return
                    match signalAvgVolume with
                    | x when x < 1.3m -> None
                    | _ ->
                        
                        let findNextSignalBar (bars: PriceBar[]) (index: int) =
                            let startIndex = index + 1
                            let endIndex = bars.Length - 1
                            
                            let indexBar = bars[index]
                            let avgVolume = barData[index] |> snd
                            
                            let isSignalBar (bar:PriceBar) =
                                let volume = decimal bar.Volume
                                let close = bar.Close
                                let indexBarOpenOrClose = min indexBar.Open indexBar.Close
                                let age = bar.Date - indexBar.Date
                                volume >= 1.3m * avgVolume
                                    && close >= indexBarOpenOrClose
                                    && close < indexBarOpenOrClose * 1.1m
                                    && age.TotalDays >= 14
                                    && age.TotalDays <= 90
                                    && bar.Open < bar.Close
                            
                            bars[startIndex..endIndex]
                            |> Array.tryFind isSignalBar
                            |> Option.map (fun bar -> r, bar, avgVolume)
                        
                        findNextSignalBar prices.Bars (index |> fst)
            }
        )
        |> Async.Sequential
        
    let filtered =
        runFilter
        |> Seq.choose id
        |> Seq.distinctBy (fun (r, bar, _) -> r.Ticker + bar.DateStr)
        |> Seq.map (fun (r, bar, _) ->
            // System.Console.WriteLine($"{r.Ticker} {r.Date} {bar.DateStr}")
            // System.Console.ReadLine() |> ignore
            SignalWithPriceProperties.Row(
                ticker=r.Ticker,
                date=bar.DateStr,
                screenerid=r.Screenerid,
                ``open``=bar.Open,
                close=bar.Close,
                high=bar.High,
                low=bar.Low,
                gap=0,
                sma20=r.Sma20,
                sma50=r.Sma50,
                sma150=r.Sma150,
                sma200=r.Sma200)
        )
        
    let output = new SignalWithPriceProperties(filtered)
    do! output.SaveToString() |> appendCsv outputFilename
}   
