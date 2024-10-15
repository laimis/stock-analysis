module coretests.fs.Options.OptionPositionTests

open System
open Xunit
open FsUnit
open core.fs.Options
open testutils

let ticker = TestDataGenerator.NET

[<Fact>]
let ``Basic operations work`` () =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.UnderlyingTicker |> should equal ticker
    position.IsClosed |> should equal false
    position.IsOpen |> should equal true
    
    let optionType = OptionType.Put
    let quantity = 1m
    
    let positionWithContracts =
        position
        |> OptionPosition.buyToOpen "2024-11-15" 120m optionType quantity 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen "2024-11-15" 115m optionType quantity 4.20m DateTimeOffset.UtcNow
        
    positionWithContracts.TotalCost |> should equal 1.85m
    positionWithContracts.Transactions |> should haveLength 2m
    positionWithContracts.IsClosed |> should equal false
    positionWithContracts.IsOpen |> should equal true
    
    let positionWithContractsClosed =
        positionWithContracts
        |> OptionPosition.sellToClose "2024-11-15" 120m optionType quantity 11.11m DateTimeOffset.UtcNow
        |> OptionPosition.buyToClose "2024-11-15" 115m optionType quantity 8.11m DateTimeOffset.UtcNow
        
    positionWithContractsClosed.TotalCost |> should equal -1.15m
    positionWithContractsClosed.Transactions |> should haveLength 4m
    positionWithContractsClosed.IsClosed |> should equal true
    positionWithContractsClosed.IsOpen |> should equal false
    
[<Fact>]
let ``Expire works``() =
    let position =
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.sellToOpen "2024-11-15" 120m OptionType.Put 1m 6.05m DateTimeOffset.UtcNow
        |> OptionPosition.expire
        
    position.IsClosed |> should equal true
    position.IsOpen |> should equal false
    
[<Fact>]
let ``Buy to open using wrong date format, fails``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.buyToOpen "Nov 15 2024" 120m OptionType.Put 1m 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typeof<Exception>
    
