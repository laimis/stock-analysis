module coretests.Stocks

open System
open Xunit
open core.fs.Shared.Domain
open coretests.testdata
open FsUnit
let userId = TestDataGenerator.RandomUserId()
let ticker = TestDataGenerator.TEUM

[<Fact>]
let ``Purchase works`` () =
    
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow None
    
    stock.Ticker |> should equal ticker
    stock.NumberOfShares |> should equal 10
    
    let afterTransitions =
        stock
        |> StockPosition.buy 5m 2m DateTimeOffset.Now None
        |> StockPosition.sell 5m 20m DateTimeOffset.Now (Some "sample note")
        
    afterTransitions.NumberOfShares |> should equal 10


[<Fact>]
let ``Selling not owned fails`` () =
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow None
    
    (fun () -> StockPosition.sell 20m 100m DateTimeOffset.UtcNow (Some "sample note") stock |> ignore)
    |> should throw typeof<Exception>


[<Fact>]
let ``Buying for zero throws`` () =
    let stock = StockPosition.openLong ticker DateTimeOffset.UtcNow
        
    (fun () -> StockPosition.buy 10m 0m DateTimeOffset.UtcNow None stock |> ignore)
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
        |> StockPosition.buy 10m 2.1m DateTimeOffset.UtcNow None
    
    let events = stock.Events
    
    let stock2 = StockPosition.createFromEvents events
    
    stock |> should equal stock2
    
[<Fact>]
let ``Multiple buys average cost correct`` () =
    let purchase1 =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow None
    
    let withCalculation = purchase1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 7.5m
    withCalculation.Cost |> should equal 15m
    
    let sell1 = StockPosition.sell 1m 6m DateTimeOffset.UtcNow None purchase1
    let withCalculation = sell1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 10m
    
    let purchase2 = StockPosition.buy 1m 10m DateTimeOffset.UtcNow None sell1
    let withCalculation = purchase2 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 20m
    
    let sell2 = StockPosition.sell 2m 10m DateTimeOffset.UtcNow None purchase2
    let withCalculation = sell2 |> StockPositionWithCalculations
    DateTimeOffset.UtcNow.Subtract(withCalculation.Closed.Value).TotalSeconds |> int |> should be (lessThan 1)
    withCalculation.DaysHeld |> should equal 0
    withCalculation.Profit |> should equal 1
    Assert.Equal(0.04m, withCalculation.GainPct, 2)
    
[<Fact>]
let ``Sell creates PL Transactions`` () =
    
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow None 
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow None
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow None
        |> StockPosition.sell 2m 10m DateTimeOffset.UtcNow None
        |> StockPositionWithCalculations
        
    let transactions = stock.PLTransactions
    
    transactions |> should haveLength 2
    
    transactions |> List.head |> _.Profit |> should equal 1m
    transactions |> List.last |> _.Profit |> should equal 0m
    
[<Fact>]
let ``Multiple buys deleting transactions`` () =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow None
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow None
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow None
        
    position.Transactions |> should haveLength 4
    position.NumberOfShares |> should equal 2m
    
    let lastTransaction = position.Transactions |> List.last
    
    let position2 = StockPosition.deleteTransaction lastTransaction.TransactionId position
    
    position2.Transactions |> should haveLength 3
    position2.NumberOfShares |> should equal 1m
    
[<Fact>]
let ``Days held is correct`` () =
    
    let position =
        StockPosition.openLong ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 5m (DateTimeOffset.UtcNow.AddDays(-5)) None
        |> StockPosition.buy 1m 10m (DateTimeOffset.UtcNow.AddDays(-2)) None
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 2
    
    let afterSell1 = position |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow None
    
    let calculated = afterSell1 |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 0
    
[<Fact>]
let ``Assigning stop works``() =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
        
    position.StopPrice.Value |> should equal 4m
    
    let events = position.Events
    
    let sameStop = position |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow
    
    events |> should equal sameStop.Events
    
[<Fact>]
let ``Assigning stop to closed position fails``() =
    
    let position = 
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow None
        
    (fun () -> position |> StockPosition.setStop (Some 4m) DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>

[<Fact>]
let ``Adding note works``() =
    
    let position =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow None
        |> StockPosition.addNotes (Some "this is a note") DateTimeOffset.UtcNow
        
    position.Notes |> should haveLength 1
    position.Notes |> List.head |> should equal "this is a note"
    
    let events = position.Events
    
    let sameNote = position |> StockPosition.addNotes (Some "this is a note") DateTimeOffset.UtcNow
    
    events |> should equal sameNote.Events
    

// [<Fact>]

//         [Fact]
//         public void DeletePosition_OnClosedPosition_Fails()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             stock.Sell(1, 6, DateTimeOffset.UtcNow, null);
//
//             Assert.Throws<InvalidOperationException>(() => 
//                 stock.DeletePosition(positionId)
//             );
//         }
//
//         [Fact]
//         public void DeletePosition_Works()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             stock.Sell(1, 6, DateTimeOffset.UtcNow, null);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId2 = stock.State.OpenPosition.PositionId;
//             stock.DeletePosition(positionId2);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId3 = stock.State.OpenPosition.PositionId;
//             stock.Sell(1, 6, DateTimeOffset.UtcNow, null);
//
//             var positions = stock.State.GetAllPositions();
//             Assert.Equal(2, positions.Count);
//             Assert.Equal(positionId, stock.State.GetAllPositions()[0].PositionId);
//             Assert.Equal(positionId3, stock.State.GetAllPositions()[1].PositionId);
//             
//             // make sure transactions don't include deleted position
//             Assert.Equal(6, stock.State.Transactions.Count);
//             Assert.Equal(2, stock.State.Transactions.Count(t => t.IsPL));
//             Assert.Equal(4, stock.State.Transactions.Count(t => !t.IsPL));
//         }
//
//         [Fact]
//         public void LabelsWork()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             
//             var set = stock.SetPositionLabel(positionId, "strategy", "newhigh");
//             Assert.True(set);
//
//             var setAgain = stock.SetPositionLabel(positionId, "strategy", "newhigh");
//             Assert.False(setAgain);
//
//             var setDifferent = stock.SetPositionLabel(positionId, "strategy", "newlow");
//             Assert.True(setDifferent);
//         }
//
//         [Fact]
//         public void SetLabel_WithNullValue_Fails()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             
//             Assert.Throws<InvalidOperationException>(() => 
//                 stock.SetPositionLabel(positionId, "strategy", null)
//             );
//         }
//
//         [Fact]
//         public void SetLabel_WithNullKey_Fails()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             
//             Assert.Throws<InvalidOperationException>(() => 
//                 stock.SetPositionLabel(positionId, null, "newhigh")
//             );
//         }
//
//         [Fact]
//         public void DeleteLabel_Works()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             
//             var set = stock.SetPositionLabel(positionId, "strategy", "newhigh");
//             Assert.True(set);
//
//             var deleted = stock.DeletePositionLabel(positionId, "strategy");
//             Assert.True(deleted);
//
//             var deletedAgain = stock.DeletePositionLabel(positionId, "strategy");
//             Assert.False(deletedAgain);
//         }
//
//         [Fact]
//         public void DeleteLabel_WithNullKey_Fails()
//         {
//             var stock = new OwnedStock(TestDataGenerator.TSLA, _userId);
//
//             stock.Purchase(1, 5, DateTimeOffset.UtcNow.AddDays(-5));
//             var positionId = stock.State.OpenPosition.PositionId;
//             
//             Assert.Throws<InvalidOperationException>(() => 
//                 stock.DeletePositionLabel(positionId, null)
//             );
//         }
//
//         [Fact]
//         public void BuyWithStopAtCost_Works()
//         {
//             var stock = new OwnedStock(_ticker, _userId);
//             stock.Purchase(1, 1, DateTimeOffset.UtcNow, notes:null, stopPrice: 1);
//             
//             Assert.Null(stock.State.OpenPosition.RiskedAmount);
//         }
//     }

// }