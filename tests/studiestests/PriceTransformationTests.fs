module studiestests.PriceTransformationTests

open Xunit
open FsUnit
open studies.ScreenerStudy
open studies.DataHelpers
open testutils

let transform rows =
    let mock = DataHelpersTests.setupGetPricesWithNoBrokerageAccess
    rows
    |> transformSignals mock DataHelpersTests.testDataPath
    
[<Fact>]
let ``Price transformation means adding price, gap, and SMA data``() = async {
    let! transformed = [Signal.Row(date = "2022-08-05", ticker = "NET", screenerid = 1)] |> transform
    
    transformed.Rows |> Seq.length |> should equal 1
    
    let row = transformed.Rows |> Seq.head
    
    row.Date |> should equal "2022-08-05"
    row.Ticker |> should equal "NET"
    row.Screenerid |> should equal 1
    row.Gap |> should be (greaterThan 0m)
    row.Sma20 |> should be (greaterThan 0m)
    row.Sma50 |> should be (greaterThan 0m)
    row.Sma150 |> should be (greaterThan 0m)
    row.Sma200 |> should be (greaterThan 0m)
}

[<Fact>]
let ``Price transformation when there is not enough sma data, adds zeros but does not fail``() = async {
    let! transformed =
        [Signal.Row(date = "2020-11-30", ticker = "NET", screenerid = 1)]
        |> transform
    
    transformed.Rows |> Seq.length |> should equal 1
    
    let row = transformed.Rows |> Seq.head
    
    row.Date |> should equal "2020-11-30"
    row.Ticker |> should equal "NET"
    row.Screenerid |> should equal 1
    row.Gap |> should equal 0m
    row.Sma20 |> should equal 0m
    row.Sma50 |> should equal 0m
    row.Sma150 |> should equal 0m
    row.Sma200 |> should equal 0m
}

[<Fact>]
let ``Transform when price information is not available, should skip that signal``() = async {
    
    let! transformed =
        [Signal.Row(date = "2022-08-05", ticker = "NET1", screenerid = 1)]
        |> transform
    
    transformed.Rows |> Seq.length |> should equal 0
}

[<Fact>]
let ``Transform retries fetching the prices until all the price feeds are available``() = async {
    
    let mutable callCount = 0
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    task {
                        
                        callCount <- callCount + 1
                        
                        return
                            match callCount with
                            | x when x >= 2 ->
                                TestDataGenerator.PriceBars(TestDataGenerator.NET) |> Ok
                            | _ ->
                                core.fs.ServiceError(DataHelpersTests.transactionRateErrorMessage) |> Error
                    }
        }
        
    let! transformed = 
        [Signal.Row(date = "2022-08-05", ticker = "ABRACADABRA", screenerid = 1)]
        |> transformSignals mock DataHelpersTests.testDataPath
        
    transformed.Rows |> Seq.length |> should equal 1
}    
    
    
    
