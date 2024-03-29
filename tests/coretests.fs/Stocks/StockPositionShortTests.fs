module coretests.fs.Stocks.StockPositionShortTests

open System
open Xunit
open FsUnit
open core.fs.Stocks
open testutils

let userId = TestDataGenerator.RandomUserId()
let ticker = TestDataGenerator.TEUM

[<Fact>]
let ``Opening short position works`` () =
    
    let position = StockPosition.``open`` ticker -10m 1.5m DateTimeOffset.UtcNow
        
    position.NumberOfShares |> should equal -10m
    position.StockPositionType |> should equal Short
    
[<Fact>]
let ``Buying shares of short position works`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        |> StockPosition.buy 5m 1.5m DateTimeOffset.UtcNow
        
    position.NumberOfShares |> should equal -5m
    position.StockPositionType |> should equal Short
    position.IsOpen |> should equal true
    
[<Fact>]
let ``Buying all shares of short position works`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 1.5m DateTimeOffset.UtcNow
        
    position.NumberOfShares |> should equal 0m
    position.StockPositionType |> should equal Short
    position.IsClosed |> should equal true
    
    
[<Fact>]
let ``Buying more shares than available in short position throws`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.buy 15m 1.5m DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``From events recreates identical state`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        |> StockPosition.buy 5m 1.5m DateTimeOffset.UtcNow
        
    let position2 =
        position.Events
        |> StockPosition.createFromEvents
        
    position2.NumberOfShares |> should equal position.NumberOfShares
    position2.StockPositionType |> should equal position.StockPositionType
    position2.IsOpen |> should equal position.IsOpen
    position2.Ticker |> should equal position.Ticker
    position2.PositionId |> should equal position.PositionId

[<Fact>]
let ``Close position works`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        |> StockPosition.close 1.5m DateTimeOffset.UtcNow
        
    position.NumberOfShares |> should equal 0m
    position.StockPositionType |> should equal Short
    position.IsClosed |> should equal true
    
[<Fact>]
let ``Assigning stop works`` () =
    
    let position =
        StockPosition.openShort ticker DateTimeOffset.UtcNow
        |> StockPosition.sell 10m 1.5m DateTimeOffset.UtcNow
        |> StockPosition.setStop (Some 1.4m) DateTimeOffset.UtcNow
        
    position.StopPrice.Value |> should equal 1.4m
    position.HasStopPrice |> should equal true
    position.RiskAmount.IsSome |> should equal true
    position.RiskAmount.Value |> should be (greaterThan 0)
    
    let events = position.Events
    
    let sameStop =
        position
        |> StockPosition.setStop (Some 1.4m) DateTimeOffset.UtcNow
    
    events |> should equal sameStop.Events
    