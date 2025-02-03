module coretests.fs.Options.OptionPositionTests

open System
open Xunit
open FsUnit
open core.fs.Options
open testutils

let ticker = TestDataGenerator.NET
let expiration =
    DateTimeOffset.UtcNow.AddMonths(2).ToString("yyyy-MM-dd")
    |> OptionExpiration.create
    
[<Fact>]
let ``Basic operations work`` () =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.UnderlyingTicker |> should equal ticker
    position.IsClosed |> should equal false
    position.IsOpen |> should equal false // haven't bought any contracts yet
    position.IsPending |> should equal false
    position.IsPendingClosed |> should equal false
    
    let optionType = OptionType.Put
    let quantity = 1
    
    let positionWithContracts =
        position
        |> OptionPosition.buyToOpen expiration 120m optionType quantity 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen expiration 115m optionType quantity 4.20m DateTimeOffset.UtcNow
        
    positionWithContracts.Cost |> should equal (Some 1.85m)
    positionWithContracts.Profit |> should equal 0m
    positionWithContracts.Transactions |> should haveLength 2m
    positionWithContracts.IsClosed |> should equal false
    positionWithContracts.IsOpen |> should equal true
    positionWithContracts.Contracts.Count |> should equal 2
    positionWithContracts.Contracts.Values
        |> Seq.iter (fun quantityAndCost ->
            let contractQuantity =
                match quantityAndCost with
                | OpenedContractQuantityAndCost(_, q, _) -> q
            contractQuantity |> abs |> should equal 1
        )
    
    let positionWithContractsClosed =
        positionWithContracts
        |> OptionPosition.sellToClose expiration 120m optionType quantity 11.11m DateTimeOffset.UtcNow
        |> OptionPosition.buyToClose expiration 115m optionType quantity 8.11m DateTimeOffset.UtcNow
        
    positionWithContractsClosed.Cost |> should equal (Some 1.85m)
    positionWithContractsClosed.Profit |> should equal 1.15m
    positionWithContractsClosed.Transactions |> should haveLength 4m
    positionWithContractsClosed.IsClosed |> should equal true
    positionWithContractsClosed.IsOpen |> should equal false
    positionWithContractsClosed.IsPending |> should equal false
    positionWithContractsClosed.IsPendingClosed |> should equal false
    positionWithContractsClosed.Contracts.Count |> should equal 2
    positionWithContractsClosed.Contracts.Values
        |> Seq.iter (fun quantityAndCost ->
            let contractQuantity =
                match quantityAndCost with
                | OpenedContractQuantityAndCost(_, q, _) -> q
            contractQuantity |> abs |> should equal 0
        )
    
[<Fact>]
let ``Expire works``() =
    let position =
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.expire expiration 120m OptionType.Put
        
    position.IsClosed |> should equal true
    position.IsOpen |> should equal false
    
[<Fact>]
let ``Assign works``() =
    let position =
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.assign expiration 120m OptionType.Put
        
    position.IsClosed |> should equal true
    position.IsOpen |> should equal false

[<Fact>]
let ``Version and events work``() =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.Version |> should equal 1
    position.Events |> should haveLength 1
    
    let optionAfterOperation =
        position |> OptionPosition.buyToOpen expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        
    optionAfterOperation.Version |> should equal 2
    optionAfterOperation.Events |> should haveLength 2
    
    let optionAfterOperation2 =
        optionAfterOperation |> OptionPosition.sellToClose expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        
    optionAfterOperation2.Version |> should equal 4 // because it should include closed event
    optionAfterOperation2.Events |> should haveLength 4

[<Fact>]
let ``This should throw invalid operation because you cannot sell top open and then sell to close``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.sellToClose expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typedefof<InvalidOperationException>
    
[<Fact>]
let ``Trying to buy to close a position that is not open, should throw``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.buyToClose expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typedefof<InvalidOperationException>
    
[<Fact>]
let ``Trying to sell to close a contract that is not owned should throw``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.sellToClose expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typedefof<InvalidOperationException>
    
[<Fact>]
let ``Trying to assign contracts that are not owned should throw``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.assign expiration 120m OptionType.Put
        |> ignore)
    |> should throw typedefof<InvalidOperationException>
    
[<Fact>]
let ``Trying to assign contracts that are not sold should throw``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.buyToOpen expiration 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.assign expiration 120m OptionType.Put
        |> ignore)
    |> should throw typedefof<InvalidOperationException>
    
[<Fact>]
let ``Buy to open using wrong date format, fails``() =
    
    (fun () ->
        let wrongDate = DateTimeOffset.UtcNow.AddDays(7).ToString("MMMM dd, yyyy") |> OptionExpiration.create
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.buyToOpen wrongDate 120m OptionType.Put 1 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typeof<FormatException>
    
[<Fact>]
let ``Expire fails if there are no contracts to expire``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.expire expiration 120m OptionType.Put
        |> ignore)
    |> should throw typeof<InvalidOperationException>
    
[<Fact>]
let ``Assign fails if there are no contracts to assign``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.assign expiration 120m OptionType.Put
        |> ignore)
    |> should throw typeof<InvalidOperationException>
    
    
[<Fact>]
let ``Setting notes works``() =
    
    let notes = "This is a test" |> Some
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.Notes |> should be Empty
    
    let positionWithNotes = position |> OptionPosition.addNotes notes DateTimeOffset.UtcNow
    
    positionWithNotes.Notes.Length |> should equal 1
    
    let note = positionWithNotes.Notes |> Seq.head |> _.content
    
    note |> should equal notes.Value

[<Fact>]
let ``Labels work``() =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.Labels |> should be Empty
    
    let testKey = "TestKey"
    let testValue = "TestValue"
    
    let positionWithLabel = position |> OptionPosition.setLabel testKey testValue DateTimeOffset.UtcNow
    
    positionWithLabel.Labels |> should haveCount 1
    
    let label = positionWithLabel.Labels[testKey]
    
    label |> should equal testValue
    
    let positionWithLabelRemoved = positionWithLabel |> OptionPosition.deleteLabel testKey DateTimeOffset.UtcNow
    
    positionWithLabelRemoved.Labels |> should be Empty

[<Fact>]
let ``Create option without purchasing the contracts should work``() =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.IsClosed |> should equal false
    position.IsOpen |> should equal false
    position.IsPending |> should equal true
    position.IsPendingClosed |> should equal false
    position.Cost |> should be (equal None)
    
    let cost = 1.45m
    
    let modifiedPosition =
        position
        |> OptionPosition.establishDesiredCost cost DateTimeOffset.UtcNow
        |> OptionPosition.createPendingBuyOrder expiration 27.5m OptionType.Call 1 DateTimeOffset.UtcNow
        |> OptionPosition.createPendingSellOrder expiration 32.5m OptionType.Call 1 DateTimeOffset.UtcNow
        |> OptionPosition.addNotes (Some "thinking about doing this again, not sure if I will get in") DateTimeOffset.UtcNow
        
    modifiedPosition.IsClosed |> should equal false
    modifiedPosition.IsOpen |> should equal false
    modifiedPosition.IsPending |> should equal true
    modifiedPosition.IsPendingClosed |> should equal false
    modifiedPosition.DesiredCost |> should equal (Some cost)
    modifiedPosition.PendingContracts |> should haveCount 2
    
    
[<Fact>]
let ``Closing pending position should work``() =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    let cost = 1.45m
    
    let modifiedPosition =
        position
        |> OptionPosition.establishDesiredCost cost DateTimeOffset.UtcNow
        |> OptionPosition.createPendingBuyOrder expiration 27.5m OptionType.Call 1 DateTimeOffset.UtcNow
        |> OptionPosition.createPendingSellOrder expiration 32.5m OptionType.Call 1 DateTimeOffset.UtcNow
        |> OptionPosition.addNotes (Some "thinking about doing this again, not sure if I will get in") DateTimeOffset.UtcNow
        
    let closedPosition =
        modifiedPosition |> OptionPosition.close (Some "after thinking about it more, decide not to persue this") DateTimeOffset.UtcNow
        
    closedPosition.IsClosed |> should equal true
    closedPosition.IsOpen |> should equal false
    closedPosition.IsPending |> should equal false
    closedPosition.IsPendingClosed |> should equal true
    closedPosition.Notes |> should haveLength 2

[<Fact>]
let ``Buying multiple contracts at different quantities maintains accurate cost``() =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    let cost = 1.45m
    
    let modifiedPosition =
        position
        |> OptionPosition.buyToOpen expiration 27.5m OptionType.Call 1 cost DateTimeOffset.UtcNow
        |> OptionPosition.buyToOpen expiration 27.5m OptionType.Call 2 cost DateTimeOffset.UtcNow
        
    modifiedPosition.Cost |> should equal (Some 4.35m)
    let quantityAndCost = modifiedPosition.Contracts.Values |> Seq.head
    match quantityAndCost with
    | OpenedContractQuantityAndCost(longOrShort, quantity, cost) ->
        Long |> should equal longOrShort
        quantity |> should equal 3
        cost |> should equal 4.35m
    
