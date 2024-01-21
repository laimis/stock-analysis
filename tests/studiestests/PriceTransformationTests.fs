module studiestests.PriceTransformationTests

open Xunit
open FsUnit
open studies.Types

let transform rows = rows |> studies.PriceTransformation.transform DataHelpersTests.setupGetPricesWithNoBrokerageAccess
    
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
    
    
    