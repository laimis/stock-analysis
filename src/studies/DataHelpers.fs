module studies.DataHelpers

open System
open System.Collections.Concurrent
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open core.fs
open core.fs.Adapters.Stocks

type IGetPriceHistory =
    abstract member GetPriceHistory : start:DateTimeOffset option -> ``end``:DateTimeOffset option -> ticker:core.Shared.Ticker -> Task<Result<PriceBars,ServiceError>>

type ISignal =
    abstract member Ticker : string
    abstract member Date : string
    abstract member Screenerid : int option
    abstract member Gap : decimal option
    abstract member Sma20 : decimal option
    abstract member Sma50 : decimal option
    abstract member Sma150 : decimal option
    abstract member Sma200 : decimal option
    
let verifySignals (records:ISignal seq) minimumRecordsExpected =
        
        // make sure there is at least some records in here, ideally in thousands
        let numberOfRecords = records |> Seq.length
        match numberOfRecords with
        | x when x < minimumRecordsExpected -> failwith $"{x} is not enough records, expecting at least {minimumRecordsExpected}"
        | _ -> ()

        let verifyCondition failureConditionFunc messageIfFound =
            records
            |> Seq.map (fun r -> match failureConditionFunc r with | true -> Some r | false -> None)
            |> Seq.choose id
            |> Seq.tryHead
            |> Option.iter (fun r -> r |> messageIfFound |> failwith)

        // make sure all the dates are set (and can be parsed?)
        let invalidDate = fun (r:ISignal) -> match DateTimeOffset.TryParse(r.Date) with | true, _ -> false | false, _ -> true
        let messageIfInvalidDate = fun (r:ISignal) -> $"date is invalid for record {r.Date}, {r.Ticker}"
        verifyCondition invalidDate messageIfInvalidDate

        let invalidTicker = fun (r:ISignal) -> String.IsNullOrWhiteSpace(r.Ticker)
        let messageIfInvalidTicker = fun (r:ISignal) -> $"ticker is blank for record {r.Screenerid}, {r.Date}"
        verifyCondition invalidTicker messageIfInvalidTicker

        let invalidScreenerId = fun (r:ISignal) -> r.Screenerid.IsSome && r.Screenerid.Value = 0
        let messageIfInvalidScreenerId = fun (r:ISignal) -> $"screenerid is blank for record {r.Ticker}, {r.Date}"
        verifyCondition invalidScreenerId messageIfInvalidScreenerId

        records
        
let describeSignals (records:ISignal seq) =

        let numberOfRecords = records |> Seq.length

        match numberOfRecords with
        | x when x > 0 ->
            let dates = records |> Seq.map (_.Date) |> Seq.distinct |> Seq.length
            let tickers = records |> Seq.map (_.Ticker) |> Seq.distinct |> Seq.length
            let screenerIds = records |> Seq.map (_.Screenerid) |> Seq.distinct |> Seq.length
            
            let minimumDate = records |> Seq.minBy (_.Date) |> _.Date
            let maximumDate = records |> Seq.maxBy (_.Date) |> _.Date

            printfn $"Records: %d{numberOfRecords}, dates: %d{dates}, tickers: %d{tickers}, screenerIds: %d{screenerIds}"
            printfn $"Minimum date: %A{minimumDate}"
            printfn $"Maximum date: %A{maximumDate}"
            printfn ""
        | _ ->
            printfn $"No records found in the input"
    
type PriceNotAvailableError =
    | NotAvailableForever of string
    | NotAvailable of string
    
    with
        static member getError (error:PriceNotAvailableError) =
            match error with
            | NotAvailableForever x -> x
            | NotAvailable x -> x

let private generatePriceCsvPath studiesDirectory ticker =
    let filename = $"{ticker}.csv"
    $"{studiesDirectory}/prices/{filename}"

let private priceCache = ConcurrentDictionary<string,PriceBars>()

let private readPricesFromCsv (path:string) = async {
        match priceCache.TryGetValue(path) with
        | true, bars -> return bars
        | false, _ ->
            if ServiceHelper.logger <> null then ServiceHelper.logger.LogInformation("Reading prices from file {path}", path)
            let! csv = System.IO.File.ReadAllLinesAsync(path) |> Async.AwaitTask
            let bars = PriceBars(csv |> Array.map PriceBar, Daily)
            priceCache.TryAdd(path, bars) |> ignore
            return bars
    }
        
let getPricesFromCsv studiesDirectory ticker = async {
        let path = generatePriceCsvPath studiesDirectory ticker
        return! path |> readPricesFromCsv
    }
    
let private notAvailableBasedOnMessage failIfNone (message:string) =
    let notAvailableType =
        match message with
        | x when x.Contains("No candles for historical prices for") -> Some (NotAvailableForever x)
        | x when x.Contains("Invalid open price") -> Some (NotAvailableForever x)
        | x when x.Contains("Invalid low price") -> Some (NotAvailableForever x)
        | x when x.Contains("Individual App's transactions per seconds restriction reached") -> Some (NotAvailable x)
        | _ -> None
    
    if notAvailableType.IsNone && failIfNone then
        failwith $"Unexpected message: {message}"
        
    notAvailableType
   
let private readPricesFromFileSystem path = async {
    match System.IO.File.Exists(path) with
    | false -> return NotAvailable "Price file missing" |> Error
    | true ->
        let content = System.IO.File.ReadAllLines(path)
        match content with
        | [||] -> return NotAvailable "Price file empty"  |> Error
        | _ ->
            let checkIfNotAvailable = content[0] |> notAvailableBasedOnMessage false
            match checkIfNotAvailable with
            | Some x -> return x |> Error
            | None ->
                let! prices = path |> readPricesFromCsv
                return Ok prices
}
                
let callLogFuncIfSetup func =
    if ServiceHelper.logger <> null then func(ServiceHelper.logger)
    
    

let getTickersWithPriceHistory studiesDirectory =
    $"{studiesDirectory}/prices"
    |> System.IO.Directory.GetFiles
    |> Array.map System.IO.Path.GetFileNameWithoutExtension
    
let tryGetPricesFromCsv studiesDirectory ticker =
    let path = generatePriceCsvPath studiesDirectory ticker
    path |> readPricesFromFileSystem

let getPricesFromBrokerageAndRecordToCsv (brokerage:IGetPriceHistory) studiesDirectory startDate endDate ticker = async {
    let path = generatePriceCsvPath studiesDirectory ticker
    
    let recordError (message:string) =
        // callLogFuncIfSetup _.LogCritical("Error getting price history for {ticker}: {error}", ticker, message)
        let csvContent = $"ERROR: {message}"
        System.IO.File.WriteAllText(path, csvContent)
    
    let recordPrices (prices:PriceBars) =
        // store prices on a filesystem, filename should contain ticker and date range
        let csv = prices.Bars |> Array.map _.ToString() |> String.concat Environment.NewLine
        System.IO.File.WriteAllText(path, csv)
        
    try
        callLogFuncIfSetup _.LogInformation("Getting price history for {ticker} from {startDate} to {endDate}", ticker, startDate, endDate)
        let! response = brokerage.GetPriceHistory startDate endDate ticker |> Async.AwaitTask
        match response with
        | Ok prices ->
            recordPrices prices
            return Ok prices
        | Error e ->
            recordError e.Message
            return notAvailableBasedOnMessage true e.Message |> Option.get |> Error
    with
        | e ->
            recordError e.Message
            return notAvailableBasedOnMessage true e.Message |> Option.get |> Error
}
                
let getPricesWithBrokerage (brokerage:IGetPriceHistory) studiesDirectory startDate endDate ticker = async {
    // check first if we have prices for this ticker and date range
    // if we do, return them
    // if we don't, get them from brokerage and store them on a filesystem
    // return them
    let! prices = tryGetPricesFromCsv studiesDirectory ticker
    
    match prices with
    | Error error ->
        match error with
        | NotAvailableForever _ -> return prices
        | NotAvailable _ -> return! getPricesFromBrokerageAndRecordToCsv brokerage studiesDirectory startDate endDate ticker
    | Ok _ -> return prices
}

let saveCsv (filename:string) content = async {
    let dir = System.IO.Path.GetDirectoryName(filename)
    if not (System.IO.Directory.Exists(dir)) then
        System.IO.Directory.CreateDirectory(dir) |> ignore
    do! System.IO.File.WriteAllTextAsync(filename, content) |> Async.AwaitTask
}

let appendCsv filename content = async {
    do! System.IO.File.AppendAllTextAsync(filename, content) |> Async.AwaitTask
}
