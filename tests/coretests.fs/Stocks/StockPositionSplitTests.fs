module coretests.fs.Stocks.StockPositionSplitTests

open System
open Xunit
open FsUnit
open core.fs.Stocks
open testutils

let ticker = TestDataGenerator.TEUM

let private openedLongAgo () =
    let opened = DateTimeOffset.UtcNow.AddDays(-30.0)
    StockPosition.``open`` ticker 1.351m 100m opened

[<Fact>]
let ``Split multiplies shares and divides price`` () =
    let position = openedLongAgo ()
    let costBefore = position.ShareTransactions |> List.sumBy (fun x -> x.NumberOfShares * x.Price)

    let after = position |> StockPosition.split 8m DateTimeOffset.UtcNow

    after.NumberOfShares |> should equal (1.351m * 8m)
    after.ShareTransactions.Head.Price |> should equal 12.5m

    // cost basis stays the same
    let costAfter = after.ShareTransactions |> List.sumBy (fun x -> x.NumberOfShares * x.Price)
    costAfter |> should equal costBefore

[<Fact>]
let ``Split adjusts stop price`` () =
    let position =
        openedLongAgo ()
        |> StockPosition.setStop (Some 80m) (DateTimeOffset.UtcNow.AddDays(-10.0))

    let after = position |> StockPosition.split 8m DateTimeOffset.UtcNow

    after.StopPrice |> should equal (Some 10m)

[<Fact>]
let ``Split does not touch risk amount`` () =
    let position =
        openedLongAgo ()
        |> StockPosition.setStop (Some 80m) (DateTimeOffset.UtcNow.AddDays(-10.0))

    let after = position |> StockPosition.split 8m DateTimeOffset.UtcNow

    after.RiskAmount |> should equal position.RiskAmount

[<Fact>]
let ``Split does not adjust transactions on or after split date`` () =
    let splitDate = DateTimeOffset.UtcNow
    let position =
        openedLongAgo ()
        |> StockPosition.buy 2m 12m splitDate // already at post-split price

    let after = position |> StockPosition.split 8m splitDate

    after.NumberOfShares |> should equal (1.351m * 8m + 2m)

[<Fact>]
let ``Split applied twice is a no-op`` () =
    let once = openedLongAgo () |> StockPosition.split 8m DateTimeOffset.UtcNow
    let twice = once |> StockPosition.split 8m DateTimeOffset.UtcNow

    twice.NumberOfShares |> should equal once.NumberOfShares
    twice.Version |> should equal once.Version

[<Fact>]
let ``Split on closed position throws`` () =
    let position =
        openedLongAgo ()
        |> StockPosition.sell 1.351m 120m DateTimeOffset.UtcNow

    (fun () -> position |> StockPosition.split 8m DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>

[<Fact>]
let ``Split with invalid ratio throws`` () =
    let position = openedLongAgo ()

    (fun () -> position |> StockPosition.split 0m DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>
    (fun () -> position |> StockPosition.split 1m DateTimeOffset.UtcNow |> ignore)
    |> should throw typeof<Exception>

[<Fact>]
let ``Create from events recreates split state`` () =
    let after = openedLongAgo () |> StockPosition.split 8m DateTimeOffset.UtcNow

    let rebuilt = StockPosition.createFromEvents after.Events

    rebuilt.NumberOfShares |> should equal after.NumberOfShares
    rebuilt.Version |> should equal after.Version
