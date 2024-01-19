module studies.DataHelpers

open System
open System.Collections.Concurrent
open FSharp.Data
open Microsoft.Extensions.Logging
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage

let getUser (storage:IAccountStorage) email = async {
    return! storage.GetUserByEmail email |> Async.AwaitTask
}

let private generatePriceCsvPath studiesDirectory ticker =
    let filename = $"{ticker}.csv"
    $"{studiesDirectory}\\{filename}"

let private priceCache = ConcurrentDictionary<string,PriceBars>()

let private readPricesFromCsv (path:string) = async {
        match priceCache.TryGetValue(path) with
        | true, bars -> return bars
        | false, _ ->
            if ServiceHelper.logger <> null then ServiceHelper.logger.LogInformation("Reading prices from file {path}", path)
            let! csv = System.IO.File.ReadAllLinesAsync(path) |> Async.AwaitTask
            let bars = csv |> Array.map PriceBar |> PriceBars
            priceCache.TryAdd(path, bars) |> ignore
            return bars
    }
        
let getPricesFromCsv studiesDirectory ticker = async {
        let path = generatePriceCsvPath studiesDirectory ticker
        return! path |> readPricesFromCsv
    }

type private PriceAvailability =
    | Available
    | NotAvailableForever
    | NotAvailable
    
let private pricesAvailableOnFileSystem path =
        match System.IO.File.Exists(path) with
        | false -> NotAvailable
        | true ->
            let content = System.IO.File.ReadAllLines(path)
            match content with
            | [||] -> NotAvailable
            | _ ->
                let firstLine = content[0]
                match firstLine with
                | x when x.Contains("No candles for historical prices for") -> NotAvailableForever
                | x when x.Contains("Invalid open price") -> NotAvailableForever
                | x when x.Contains("ERROR") -> NotAvailable
                | _ -> Available
                
let callLogFuncIfSetup func =
    if ServiceHelper.logger <> null then func(ServiceHelper.logger)
                
let getPricesWithBrokerage (user:User) (brokerage:IBrokerageGetPriceHistory) studiesDirectory startDate endDate ticker = async {
    
    // check first if we have prices for this ticker and date range
    // if we do, return them
    // if we don't, get them from brokerage and store them on a filesystem
    // return them
    let path = generatePriceCsvPath studiesDirectory ticker
    
    let recordError message =
        callLogFuncIfSetup _.LogCritical("Error getting price history for {ticker}: {error}", ticker, message)
        let csvContent = $"ERROR: {message}"
        System.IO.File.WriteAllText(path, csvContent)
    
    let recordPrices (prices:PriceBars) =
        // store prices on a filesystem, filename should contain ticker and date range
        let csv = prices.Bars |> Array.map _.ToString() |> String.concat Environment.NewLine
        System.IO.File.WriteAllText(path, csv)
        
    match pricesAvailableOnFileSystem(path) with
    | Available ->
        let! prices = path |> readPricesFromCsv
        return prices |> Some
        
    | NotAvailableForever ->
        return None
    
    | NotAvailable ->
        try
            callLogFuncIfSetup _.LogInformation("Getting price history for {ticker} from {startDate} to {endDate}", ticker, startDate, endDate)
            let! response = brokerage.GetPriceHistory user.State ticker PriceFrequency.Daily startDate endDate |> Async.AwaitTask
            let result = response.Result
            match result with
            | Ok prices ->
                recordPrices prices
                return Some prices
            | Error e ->
                recordError e.Message
                return None
        with
            | e ->
                recordError e.Message
                return None
}

let getScreenerResults screenerExportUrl = async {
    let! response = Http.AsyncRequest(screenerExportUrl)
    return
        match response.Body with
        | Text text -> text
        | _ -> failwith "Unexpected response from screener"
}

let saveCsv filename content = async {
    do! System.IO.File.WriteAllTextAsync(filename, content) |> Async.AwaitTask
}

let appendCsv filename content = async {
    do! System.IO.File.AppendAllTextAsync(filename, content) |> Async.AwaitTask
}