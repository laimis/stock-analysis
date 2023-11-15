module studies.DataHelpers

open System
open System.Collections.Generic
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts
open Microsoft.Extensions.Logging

let getUser (storage:IAccountStorage) email = async {
    return! storage.GetUserByEmail email |> Async.AwaitTask
}


let generatePriceCsvPath studiesDirectory ticker =
    let filename = $"{ticker}.csv"
    $"{studiesDirectory}\\{filename}"

let private priceCache = Dictionary<string,PriceBars>()

let readPricesFromCsv (path:string) =
    match priceCache.TryGetValue(path) with
    | true, bars -> bars
    | false, _ ->
        ServiceHelper.logger.LogInformation("Reading prices from file {path}", path)
        let csv = System.IO.File.ReadAllLines(path)
        let bars = csv |> Array.map PriceBar |> PriceBars
        priceCache.Add(path, bars)
        bars
        
// start / end date are here because we might be dealing with interfaces
// that are passing dates but we always return just what we have
let getPricesFromCsv studiesDirectory ticker =
    let path = generatePriceCsvPath studiesDirectory ticker
    path |> readPricesFromCsv
  

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
                
let getPricesWithBrokerage (user:User) (brokerage:IBrokerage) studiesDirectory startDate endDate ticker = async {
    
    // check first if we have prices for this ticker and date range
    // if we do, return them
    // if we don't, get them from brokerage and store them on a filesystem
    // return them
    let path = generatePriceCsvPath studiesDirectory ticker
    
    let recordError message =
        ServiceHelper.logger.LogCritical("Error getting price history for {ticker}: {error}", ticker, message)
        let csvContent = $"ERROR: {message}"
        System.IO.File.WriteAllText(path, csvContent)
    
    let recordPrices (prices:PriceBars) =
        // store prices on a filesystem, filename should contain ticker and date range
        let csv = prices.Bars |> Array.map (fun p -> p.ToString()) |> String.concat Environment.NewLine
        System.IO.File.WriteAllText(path, csv)
        
    match pricesAvailableOnFileSystem(path) with
    | Available -> return path |> readPricesFromCsv |> Some
    | NotAvailableForever -> return None
    | NotAvailable ->
        try
            ServiceHelper.logger.LogInformation("Getting price history for {ticker} from {startDate} to {endDate}", ticker, startDate, endDate)
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