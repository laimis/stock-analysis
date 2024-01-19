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

    let user = User.Create("email", "first", "last")
    
    let! priceBars = DataHelpers.getPricesWithBrokerage
                         user mock testDataPath DateTimeOffset.UtcNow DateTimeOffset.UtcNow TestDataGenerator.NET
    
    priceBars |> Option.isSome |> should equal true
    priceBars.Value.Length |> should equal 505
}

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
let ``Get prices with brokerage should go to brokerage if price does not exists on the file system``() = async {
    
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
        
    let user = User.Create("email", "first", "last")
    
    let getPriceBars =
        DataHelpers.getPricesWithBrokerage
            user mock testDataPath DateTimeOffset.UtcNow DateTimeOffset.UtcNow
    
    let ticker = Random() |> TestDataGenerator.GenerateRandomTicker
    
    let! priceBars = ticker |> getPriceBars
    
    priceBars |> Option.isSome |> should equal true
    priceBars.Value.Length |> should equal 1
    
    // let's call it again and ensure that it brokerage will not be bothered because DataHelpers cache price data on disk
    let! _ = ticker |> getPriceBars
    
    callCount |> should equal 1
    
}

[<Fact(Skip="Need to implement")>]
let ``Make sure error is recorded if brokerage fails``() =
    ()