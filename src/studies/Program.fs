// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts
open studies
open web

let studiesDirectory = "d:\studies"

let getUser (storage:IAccountStorage) email = async {
    return! storage.GetUserByEmail email |> Async.AwaitTask
}

type PriceAvailability =
    | Available
    | NotAvailableForever
    | NotAvailable

let getPrices (user:User) (brokerage:IBrokerage) startDate endDate ticker = async {
    
    // check first if we have prices for this ticker and date range
    // if we do, return them
    // if we don't, get them from brokerage and store them on a filesystem
    // return them
    let filename = $"{ticker}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv"
    let path = $"{studiesDirectory}\\{filename}"
    
    let recordError message =
        ServiceHelper.logger.LogCritical("Error getting price history for {ticker}: {error}", ticker, message)
        let csvContent = $"ERROR: {message}"
        System.IO.File.WriteAllText(path, csvContent)
    
    let recordPrices (prices:PriceBars) =
        // store prices on a filesystem, filename should contain ticker and date range
        let csv = prices.Bars |> Array.map (fun p -> p.ToString()) |> String.concat Environment.NewLine
        System.IO.File.WriteAllText(path, csv)
        
    let readPricesFromCsv() =
        ServiceHelper.logger.LogInformation("Reading prices from file {path}", path)
        let csv = System.IO.File.ReadAllLines(path)
        let bars = csv |> Array.map PriceBar |> PriceBars
        bars
        
    let pricesAvailableOnFileSystem() =
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
        
    match pricesAvailableOnFileSystem() with
    | Available -> return readPricesFromCsv() |> Some
    | NotAvailableForever -> return None
    | NotAvailable ->
        try
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

Environment.GetCommandLineArgs() |> ServiceHelper.init

let user = "laimis@gmail.com" |> getUser (ServiceHelper.storage()) |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let inputFilename = $"{studiesDirectory}\\01_export_date_ticker_screenerid.csv"

let priceFunction = getPrices user.Value (ServiceHelper.brokerage())

GapStudy.run inputFilename priceFunction |> Async.RunSynchronously