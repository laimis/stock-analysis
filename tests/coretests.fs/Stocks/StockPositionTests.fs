module coretests.Stocks

open System
open Xunit
open core.Shared
open core.fs.Stocks
open coretests.testdata
open FsUnit

let userId = TestDataGenerator.RandomUserId()
let ticker = TestDataGenerator.TEUM

[<Fact>]
let ``Purchase works`` () =
    
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow
    
    stock.Ticker |> should equal ticker
    stock.NumberOfShares |> should equal 10
    stock.IsClosed |> should equal false
    stock.StopPrice |> should equal None
    stock.IsOpen |> should equal true
    
    let afterTransitions =
        stock
        |> StockPosition.buy 5m 2m DateTimeOffset.Now
        |> StockPosition.sell 5m 20m DateTimeOffset.Now
        
    afterTransitions.NumberOfShares |> should equal 10


[<Fact>]
let ``Selling not owned fails`` () =
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow
    
    (fun () -> StockPosition.sell 20m 100m DateTimeOffset.UtcNow stock |> ignore)
    |> should throw typeof<Exception>


[<Fact>]
let ``Buying for zero throws`` () =
    let stock = StockPosition.openLong ticker DateTimeOffset.UtcNow
        
    (fun () -> StockPosition.buy 10m 0m DateTimeOffset.UtcNow stock |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Buying with bad date throws`` () =
    (fun () -> StockPosition.openLong ticker DateTimeOffset.MinValue |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Buying with date in the future throws`` () =
    (fun () -> StockPosition.openLong ticker (DateTimeOffset.UtcNow.AddDays(1.0)) |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Create from events recreates identical state`` () =
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow
    
    let events = stock.Events
    
    let stock2 = StockPosition.createFromEvents events
    
    stock |> should equal stock2
    
[<Fact>]
let ``Multiple buys average cost correct`` () =
    let purchase1 =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
    
    let withCalculation = purchase1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 7.5m
    withCalculation.Cost |> should equal 15m
    
    let sell1 = StockPosition.sell 1m 6m DateTimeOffset.UtcNow purchase1
    let withCalculation = sell1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 10m
    
    let purchase2 = StockPosition.buy 1m 10m DateTimeOffset.UtcNow sell1
    let withCalculation = purchase2 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 20m
    
    let sell2 = StockPosition.sell 2m 10m DateTimeOffset.UtcNow purchase2
    sell2.IsClosed |> should equal true
    let withCalculation = sell2 |> StockPositionWithCalculations
    DateTimeOffset.UtcNow.Subtract(withCalculation.Closed.Value).TotalSeconds |> int |> should be (lessThan 1)
    withCalculation.DaysHeld |> should equal 0
    withCalculation.Profit |> should equal 1
    
    Assert.Equal(0.04m, withCalculation.GainPct, 2)
    
[<Fact>]
let ``Sell creates PL Transactions`` () =
    
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow 
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
        |> StockPosition.sell 2m 10m DateTimeOffset.UtcNow
        |> StockPositionWithCalculations
        
    let transactions = stock.PLTransactions
    
    transactions |> should haveLength 2
    
    transactions |> List.head |> _.Profit |> should equal 1m
    transactions |> List.last |> _.Profit |> should equal 0m
    
[<Fact>]
let ``Multiple buys deleting transactions`` () =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
        
    position.Transactions |> should haveLength 4
    position.NumberOfShares |> should equal 2m
    
    let lastTransaction =
        position.Transactions
        |> List.map (fun t -> match t with | Share t -> Some t | _ -> None)
        |> List.choose id
        |> List.last
        
    let position2 = StockPosition.deleteTransaction lastTransaction.TransactionId DateTimeOffset.UtcNow position
    
    position2.Transactions |> should haveLength 3
    position2.NumberOfShares |> should equal 1m
    
[<Fact>]
let ``Days held is correct`` () =
    
    let position =
        StockPosition.openLong ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 5m (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 10m (DateTimeOffset.UtcNow.AddDays(-2))
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 2
    
    let afterSell1 = position |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
    
    let calculated = afterSell1 |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 0
    
[<Fact>]
let ``Assigning stop works``() =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
        
    position.StopPrice.Value |> should equal 4m
    position.HasStopPrice |> should equal true
    
    let events = position.Events
    
    let sameStop = position |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
    
    events |> should equal sameStop.Events
    
[<Fact>]
let ``Delete stop works``() =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
        
    position.HasStopPrice |> should equal true
    
    let afterDeletion = position |> StockPosition.deleteStop DateTimeOffset.UtcNow
    
    afterDeletion.StopPrice |> should equal None
    afterDeletion.HasStopPrice |> should equal false
    
[<Fact>]
let ``Delete stop on closed position fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.deleteStop DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Delete stop on position without stop does nothing`` () =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        
    let sameStop = position |> StockPosition.deleteStop DateTimeOffset.UtcNow
    
    sameStop.StopPrice |> should equal None
    sameStop.HasStopPrice |> should equal false
    
    position.Events |> should equal sameStop.Events

[<Fact>]
let ``Adding note works``() =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.addNotes (Some "this is a note") DateTimeOffset.UtcNow
        
    position.Notes |> should haveLength 1
    position.Notes |> List.head |> should equal "this is a note"
    
    let events = position.Events
    
    let sameNote = position |> StockPosition.addNotes (Some "this is a note") DateTimeOffset.UtcNow
    
    events |> should equal sameNote.Events
    
[<Fact>]
let ``Labels work``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.setLabel "strategy" "newhigh" DateTimeOffset.UtcNow
        
    position.Labels["strategy"] |> should equal "newhigh"
    
    let events = position.Events
    
    let sameLabel = position |> StockPosition.setLabel "strategy" "newhigh" DateTimeOffset.UtcNow
    
    events |> should equal sameLabel.Events
    
    let differentLabel = position |> StockPosition.setLabel "strategy" "newlow" DateTimeOffset.UtcNow
    
    differentLabel.Labels["strategy"] |> should equal "newlow"
    
[<Fact>]
let ``Set label with null value fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.setLabel "strategy" null DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Set label with null key fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.setLabel null "newhigh" DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>

[<Fact>]
let ``Delete label works``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.setLabel "strategy" "newhigh" DateTimeOffset.UtcNow
        
    position.Labels["strategy"] |> should equal "newhigh"
    
    let deleted = position |> StockPosition.deleteLabel "strategy" DateTimeOffset.UtcNow
    
    deleted.Labels |> should be Empty
    
    let events = deleted.Events
    
    let deletedAgain = deleted |> StockPosition.deleteLabel "strategy" DateTimeOffset.UtcNow
    
    events |> should equal deletedAgain.Events

[<Fact>]
let ``Delete label with null key fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.deleteLabel null DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>

[<Fact>]
let ``Buy with stop at cost works``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 1m DateTimeOffset.UtcNow
        |> StockPosition.setStop (Some 1m) DateTimeOffset.UtcNow
        |> StockPositionWithCalculations
        
    position.RiskedAmount.Value |> should equal 0m
   
    
[<Fact>]
let ``Assign grade to open position should fail`` () =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 1m DateTimeOffset.UtcNow
        
    (fun () -> position |> StockPosition.assignGrade (TradeGrade("A")) (Some "this trade went perfectly!") DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>
    
[<Fact>]
let ``Assign grade to closed position should succeed`` () =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 1m DateTimeOffset.UtcNow
        |> StockPosition.sell 1m 2m DateTimeOffset.UtcNow
        |> StockPosition.assignGrade (TradeGrade("A")) (Some "this trade went perfectly!") DateTimeOffset.UtcNow
        
    position.Grade |> should equal (Some (TradeGrade("A")))
    position.Notes |> should contain "this trade went perfectly!"
    position.GradeNote.Value |> should be (equal "this trade went perfectly!")

[<Fact>]
let ``Assign grade to graded position, updates grade and note`` () =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 1m DateTimeOffset.UtcNow
        |> StockPosition.sell 1m 2m DateTimeOffset.UtcNow
        |> StockPosition.assignGrade (TradeGrade("A")) (Some "this trade went perfectly!") DateTimeOffset.UtcNow
        |> StockPosition.assignGrade (TradeGrade("B")) (Some "this trade went perfectly! (updated)") DateTimeOffset.UtcNow
        
    position.Grade |> should equal (Some (TradeGrade("B")))
    position.Notes |> should contain "this trade went perfectly! (updated)"
    position.GradeNote.Value |> should be (equal "this trade went perfectly! (updated)")

[<Fact>]
let ``Assign invalid grade, fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 1m DateTimeOffset.UtcNow
        |> StockPosition.sell 1m 2m DateTimeOffset.UtcNow
    
    (fun () -> position |> StockPosition.assignGrade (TradeGrade("L")) (Some "this trade went perfectly!") DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<ArgumentException>