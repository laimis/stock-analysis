module coretests.fs.Options.OptionPositionTests

open System
open Xunit
open FsUnit
open core.fs.Options
open testutils

let ticker = TestDataGenerator.NET

[<Fact>]
let ``Opening works`` () =
    
    let position = OptionPosition.``open`` ticker DateTimeOffset.UtcNow
    
    position.UnderlyingTicker |> should equal ticker
    position.IsClosed |> should equal false
    position.IsOpen |> should equal true
    
    let expiration = "2024-11-15"
    let strike = 120m
    let optionType = OptionType.Put
    let quantity = 1
    let price = 6.05m
    
    // add a leg
    let positionWithLeg =
        position |>
        OptionPosition.buyLeg expiration strike optionType quantity price
        
    positionWithLeg.Legs |> should haveCount 1
