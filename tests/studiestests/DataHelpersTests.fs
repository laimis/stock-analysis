module studiestests.DataHelpersTests

open System
open System.IO
open Xunit
open FsUnit
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open studies
open testutils

let testDataPath = TestDataGenerator.TestDataPath

let setupGetPricesWithBrokerageMock mock =
    let user = User.Create("email", "first", "last")
    
    DataHelpers.getPricesWithBrokerage user mock testDataPath
    
let setupGetPricesWithNoBrokerageAccess =
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` = task {
                    return [||] |> PriceBars |> core.fs.ServiceResponse<PriceBars>
                }
        }
        
    setupGetPricesWithBrokerageMock mock

[<Fact>]
let ``Append csv, appends instead of overwriting`` () = async {
    let path = Path.GetTempFileName()
    
    File.WriteAllText(path, "test")
    
    do! DataHelpers.appendCsv path "abc"
    
    let lines = File.ReadAllText path
    
    lines |> should equal "testabc"
    
    File.Delete path
}

[<Fact>]
let ``Save csv, overwrites instead of appending`` () = async {
    let path = Path.GetTempFileName()
    
    File.WriteAllText(path, "test")
    
    do! DataHelpers.saveCsv path "abc"
    
    let lines = File.ReadAllText path
    
    lines |> should equal "abc"
    
    File.Delete path
}

    
[<Fact>]
let ``Reading prices from csv works`` () = async {
    
    let! priceBars = DataHelpers.getPricesFromCsv testDataPath TestDataGenerator.NET
    
    priceBars.Length |> should equal 505
    
    // read second time to hit cache path ... not sure how to test explicitly that it hit cache
    let! priceBars = DataHelpers.getPricesFromCsv testDataPath TestDataGenerator.NET
    
    priceBars.Length |> should equal 505
}

[<Fact>]
let ``Get prices with brokerage should not go to brokerage if price exists on file system``() = async {
    
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    failwith "Should not have called brokerage"
        }

    let! priceBars = setupGetPricesWithBrokerageMock mock DateTimeOffset.UtcNow DateTimeOffset.UtcNow TestDataGenerator.NET
    
    priceBars |> Option.isSome |> should equal true
    priceBars.Value.Length |> should equal 505
}

[<Fact>]
let ``Get prices with brokerage should go to brokerage if price does not exists on the file system``() = async {
    ServiceHelperTests.initServiceHelper [||]
    
    let mutable callCount = 0
        
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    task {
                        callCount <- callCount + 1
                        
                        let bars =
                            [|PriceBar(DateTimeOffset.UtcNow, 1.0M, 1.0M, 1.0M, 1.0M, 1)|]
                            |> PriceBars
                            
                        return bars |> core.fs.ServiceResponse<PriceBars>
                    }
        }
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock DateTimeOffset.UtcNow DateTimeOffset.UtcNow
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Option.isSome |> should equal true
    priceBars.Value.Length |> should equal 1
    
    // let's call it again and ensure that it brokerage will not be bothered because DataHelpers cache price data on disk
    let! _ = ticker |> getPriceBars
    
    callCount |> should equal 1
    
}

[<Fact>]
let ``Make sure error is recorded if brokerage fails``() = async {
    
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    task {
                        return core.fs.ServiceError("error") |> core.fs.ServiceResponse<PriceBars>
                    }
        }
        
    let getPriceBars = setupGetPricesWithBrokerageMock mock DateTimeOffset.UtcNow DateTimeOffset.UtcNow
            
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    // feels like this test knows too much, but have no other good way to test this
    let priceFile = Path.Combine(testDataPath, ticker.Value + ".csv")
    
    let contents = File.ReadAllText priceFile
    
    contents |> should equal "ERROR: error"
    priceBars |> Option.isNone |> should equal true
}

[<Fact>]
let ``When prices are not available for perpetuity, brokerage is not pinged``() = async {
    let mutable call = 0
    
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    task {
                        call <- call + 1
                        return core.fs.ServiceError("No candles for historical prices for " + ticker.Value) |> core.fs.ServiceResponse<PriceBars>
                    }
        }
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock DateTimeOffset.UtcNow DateTimeOffset.UtcNow
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Option.isNone |> should equal true
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Option.isNone |> should equal true
    
    call |> should equal 1
}

[<Fact>]
let ``If getting price data throws, it gets recorded``() = async {
    ServiceHelperTests.initServiceHelper [||]
    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    failwith "record this error"
        }
    
    let getPriceBars = setupGetPricesWithBrokerageMock mock DateTimeOffset.UtcNow DateTimeOffset.UtcNow
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    // feels like this test knows too much, but have no other good way to test this
    let priceFile = Path.Combine(testDataPath, ticker.Value + ".csv")
    
    let contents = File.ReadAllText priceFile
    
    contents |> should equal "ERROR: record this error"
    priceBars |> Option.isNone |> should equal true
}