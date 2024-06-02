module studiestests.DataHelpersTests

open System
open System.IO
open Xunit
open FsUnit
open core.fs.Adapters.Stocks
open studies
open studies.DataHelpers
open testutils

let testDataPath = TestDataGenerator.TestDataPath
let transactionRateErrorMessage = "Individual App's transactions per seconds restriction reached"
    
let setupGetPricesWithBrokerageMock mock = getPricesWithBrokerage mock testDataPath
    
let setupGetPricesWithNoBrokerageAccess =
    {
        new IGetPriceHistory with 
            member this.GetPriceHistory start ``end`` ticker = task {
                return [||] |> PriceBars |> Ok
            }
    }
        
    // setupGetPricesWithBrokerageMock mock

[<Fact>]
let ``Append csv, appends instead of overwriting`` () = async {
    let path = Path.GetTempFileName()
    
    File.WriteAllText(path, "test")
    
    do! appendCsv path "abc"
    
    let lines = File.ReadAllText path
    
    lines |> should equal "testabc"
    
    File.Delete path
}

[<Fact>]
let ``Save csv, overwrites instead of appending`` () = async {
    let path = Path.GetTempFileName()
    
    File.WriteAllText(path, "test")
    
    do! saveCsv path "abc"
    
    let lines = File.ReadAllText path
    
    lines |> should equal "abc"
    
    File.Delete path
}
    
[<Fact>]
let ``Reading prices from csv works`` () = async {
    
    let! priceBars = getPricesFromCsv testDataPath TestDataGenerator.NET
    
    priceBars.Length |> should equal 505
    
    // read second time to hit cache path ... not sure how to test explicitly that it hit cache
    let! priceBars = getPricesFromCsv testDataPath TestDataGenerator.NET
    
    priceBars.Length |> should equal 505
}

[<Fact>]
let ``Get prices with brokerage should not go to brokerage if price exists on file system``() = async {
    
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    failwith "Should not have called brokerage"
        }

    let! priceBars = setupGetPricesWithBrokerageMock mock None None TestDataGenerator.NET
    
    match priceBars with
    | Ok priceBars -> priceBars.Length |> should equal 505
    | Error error -> error |> PriceNotAvailableError.getError |> failwith
}

[<Fact>]
let ``Get prices with brokerage should go to brokerage if price does not exists on the file system``() = async {
    ServiceHelperTests.initServiceHelper [||] |> ignore
    
    let mutable callCount = 0
        
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    task {
                        callCount <- callCount + 1
                        
                        let bars =
                            [|PriceBar(DateTimeOffset.UtcNow, 1.0M, 1.0M, 1.0M, 1.0M, 1)|]
                            |> PriceBars
                            
                        return bars |> Ok
                    }
        }
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock None None
    
    let! priceBars = ticker |> getPriceBars
    
    match priceBars with
    | Ok priceBars -> priceBars.Length |> should equal 1
    | Error error -> error |> PriceNotAvailableError.getError |> failwith
    
    // let's call it again and ensure that it brokerage will not be bothered because DataHelpers cache price data on disk
    let! _ = ticker |> getPriceBars
    
    callCount |> should equal 1
    
}

[<Fact>]
let ``Make sure error is recorded if brokerage fails``() = async {
    
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    task {
                        return core.fs.ServiceError(transactionRateErrorMessage) |> Error
                    }
        }
        
    let getPriceBars = setupGetPricesWithBrokerageMock mock None None
            
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    match priceBars with
    | Ok _ -> failwith "Should have failed"
    | Error error -> error |> PriceNotAvailableError.getError |> should equal $"{transactionRateErrorMessage}"
}

[<Fact>]
let ``When prices are not available for perpetuity, brokerage is not pinged``() = async {
    let mutable call = 0
    
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    task {
                        call <- call + 1
                        return core.fs.ServiceError("No candles for historical prices for " + ticker.Value) |> Error
                    }
        }
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock None None
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Result.isError |> should equal true
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Result.isError |> should equal true
    
    call |> should equal 1
}

[<Fact>]
let ``If getting price data throws, it gets recorded``() = async {
    ServiceHelperTests.initServiceHelper [||] |> ignore
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    failwith transactionRateErrorMessage
        }
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock None None
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    match priceBars with
    | Ok _ -> failwith "Should have failed"
    | Error error -> error |> PriceNotAvailableError.getError |> should equal transactionRateErrorMessage
}
