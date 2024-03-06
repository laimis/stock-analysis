module studies.MarketTrendsStudy

open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis

let calculateEMA (prices:decimal array) (length:int) =
    let alpha = 2m / (decimal (length + 1))
    let initialEMA = Array.averageBy id prices[..(length - 1)]

    prices
    |> Array.skip length
    |> Array.scan
        (fun prevEMA price ->
            let newEMA = alpha * price + (1m - alpha) * prevEMA
            newEMA)
        initialEMA
        
    
let run() =
    
    // get the user
    let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()
    
    // get the brokerage
    let brokerage = ServiceHelper.brokerage()
    
    // get prices for SPY for the last 10 years
    let prices =
        brokerage.GetPriceHistory user.Value.State ("SPY" |> Ticker) PriceFrequency.Daily (System.DateTimeOffset.Now.AddYears(-10) |> Some) None
        |> Async.AwaitTask
        |> Async.RunSynchronously
        
    match prices with
    | Error e -> failwith e.Message
    | Ok prices ->
        
        // calculate 20 and 50 day moving averages
        let sma = MovingAveragesContainer.Generate prices
        
        Array.mapi2 (fun i ema20Val sma50val -> i, ema20Val |> Option.defaultValue 0m, sma50val |> Option.defaultValue 0m) sma.ema20.Values sma.sma50.Values
        |> Array.iter (fun (i,ema20,sma50) ->
            let isCurrentlyUpstream = ema20 > sma50
            let isPreviouslyUpstream =
                match i with
                | 0 -> isCurrentlyUpstream
                | i -> sma.ema20.Values.[i-1] > sma.sma50.Values.[i-1]
                
            match isCurrentlyUpstream, isPreviouslyUpstream with
            | true, false -> printfn "Upstream trend detected on %A" prices.Bars[i].Date
            | false, true -> printfn "Downstream trend detected at index %A" prices.Bars[i].Date
            | _, _ -> ()
        )
