module studies.MarketTrendsStudy

open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis

type Trend = {
    start: PriceBar
    startIndex: int
    end_: PriceBar
    endIndex: int
}
    with
        member this.NumberOfDays = (this.end_.Date - this.start.Date).TotalDays |> int
        member this.NumberOfBars = this.endIndex - this.startIndex
    
let run() =
    
    // get the user
    let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()
    
    // enter ticker
    System.Console.WriteLine("Enter ticker:")
    let ticker = System.Console.ReadLine()
    
    // enter number of years
    System.Console.WriteLine("Enter number of years:")
    let years = System.Console.ReadLine() |> int
    
    // confirm the input
    System.Console.WriteLine($"Ticker: {ticker}, Years: {years}")
    
    // get the brokerage
    let brokerage = ServiceHelper.brokerage()
    
    // get prices for SPY for the last 20 years
    let prices =
        brokerage.GetPriceHistory user.Value.State (ticker |> Ticker) PriceFrequency.Daily (System.DateTimeOffset.Now.AddYears(-years) |> Some) None
        |> Async.AwaitTask
        |> Async.RunSynchronously
        
    match prices with
    | Error e -> failwith e.Message
    | Ok prices ->
        
        // calculate 20 and 50 day moving averages
        let sma = MovingAveragesContainer.Generate prices
        
        let indexedEma20AndSma50 =
            Array.mapi2 (fun i ema20Val sma50val -> i, ema20Val, sma50val) sma.ema20.Values sma.sma50.Values
            |> Array.filter (fun (_,ema20,sma50) -> ema20.IsSome && sma50.IsSome)
            |> Array.map (fun (i,ema20,sma50) -> i, ema20.Value, sma50.Value)
            
        // find first where ema20 crossed down sma50
        let firstDownstreamTrend =
            indexedEma20AndSma50
            |> Array.findIndex (fun (_,ema20,sma50) -> ema20 < sma50)
            
        // then find first where ema20 crossed up sma50 from that point
        let firstUpstreamTrend =
            indexedEma20AndSma50
            |> Array.skip firstDownstreamTrend
            |> Array.findIndex (fun (i,ema20,sma50) -> i > firstDownstreamTrend && ema20 > sma50)
        
        let createCycle foundLocation =
            let indexOfInterest, _, _ = indexedEma20AndSma50[foundLocation]
        
            {
                start = prices.BarsWithIndex[indexOfInterest] |> snd
                startIndex = prices.BarsWithIndex[indexOfInterest] |> fst
                end_ = prices.BarsWithIndex[indexOfInterest] |> snd
                endIndex = prices.BarsWithIndex[indexOfInterest] |> fst
            }
            
        let initialCycle = createCycle (firstDownstreamTrend + firstUpstreamTrend)
       
        let cycleAndCycles =
            indexedEma20AndSma50
            |> Array.skip (firstDownstreamTrend + firstUpstreamTrend)
            |> Array.fold (fun (cycle, cycles) (i, ema20, sma50) ->
                match cycle with
                | Some cycle ->
                    // checking if the cycle is ending
                    match ema20 < sma50 with
                    | true ->
                        let newCycle = { cycle with end_ = prices.BarsWithIndex[i] |> snd; endIndex = prices.BarsWithIndex[i] |> fst }
                        None, cycles @ [newCycle]
                    | false ->
                        Some cycle, cycles
                | None ->
                    match ema20 > sma50 with
                    | true ->
                        let newCycle = { start = prices.BarsWithIndex[i] |> snd; startIndex = prices.BarsWithIndex[i] |> fst; end_ = prices.BarsWithIndex[i] |> snd; endIndex = prices.BarsWithIndex[i] |> fst }
                        Some newCycle, cycles
                    | false ->
                        None, cycles
            ) (initialCycle |> Some, [])
            
        let cycle, cycles = cycleAndCycles
        
        let cycles =
            match cycle with
            | Some cycle ->
                // the cycle is continuing, so we need to add it to the list
                // use the latest bar as the end of the cycle
                let newCycle = { cycle with end_ = prices.BarsWithIndex[prices.BarsWithIndex.Length - 1] |> snd; endIndex = prices.BarsWithIndex[prices.BarsWithIndex.Length - 1] |> fst }
                cycles @ [newCycle]
            | None -> cycles
        
        System.Console.WriteLine("Discovered cycles:")
        cycles
        |> List.iter (fun cycle ->
            let description = $"{cycle.start.DateStr} - {cycle.end_.DateStr} ({cycle.NumberOfDays} days, {cycle.NumberOfBars} bars)"
            System.Console.WriteLine(description)
        )
