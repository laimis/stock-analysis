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
    let quantity = 1m
    let price = 6.05m
    
    // add a leg
    let positionWithLeg = position |> OptionPosition.buyToOpen expiration strike optionType quantity price DateTimeOffset.UtcNow
        
    positionWithLeg.Legs |> should haveLength 1
    
[<Fact>]
let ``Buy to open using wrong date format, fails``() =
    (fun () ->
        OptionPosition.``open`` ticker DateTimeOffset.UtcNow
        |> OptionPosition.buyToOpen "Nov 15 2024" 120m OptionType.Put 1m 6.05m DateTimeOffset.UtcNow
        |> ignore)
    |> should throw typeof<Exception>
    
