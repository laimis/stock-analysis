module coretests.fs.Stocks.PriceBarTests

open Xunit
open FsUnit
open testutils

let bars = TestDataGenerator.PriceBars(TestDataGenerator.NET)
let bar = bars.First

[<Fact>]
let ``Closing range works``() =
    Assert.Equal(0.75m, bar.ClosingRange(), 2);

let ``Percent difference from high works``() =
    Assert.Equal(0.02m, bar.PercentDifferenceFromHigh(bar.Close), 2)
    
[<Fact>]
let ``Percent difference from low works``() =
    Assert.Equal(-0.07m, bar.PercentDifferenceFromLow(bar.Close), 2)

[<Fact>]
let ``DateStr works``() =
    Assert.Equal("2020-11-30", bar.DateStr)

[<Fact>]
let ``True range works``() =
    Assert.Equal(6.67m, bar.TrueRange(None), 2)

[<Fact>]
let ``Length works``() =
    bars.Length |> should equal 505
    
[<Fact>]
let ``AllButLast works``() =
    bars.AllButLast().Length |> should equal 504
    bars.AllButLast().First.DateStr |> should equal "2020-11-30"
    bars.AllButLast().Last.DateStr |> should equal "2022-11-29"

[<Fact>]
let ``First works``() =
    bars.First.DateStr |> should equal "2020-11-30"

[<Fact>]
let ``Last works``() =
    bars.Last.DateStr |> should equal "2022-11-30"

[<Fact>]
let ``LatestOrAll with smaller range works``() =
    let subset = bars.LatestOrAll(numberOfBars = 20)
    subset.Length |> should equal 20
    subset.Last.DateStr |> should equal "2022-11-30"
    subset.First.DateStr |> should equal "2022-11-02"

[<Fact>]
let ``Closing Prices accurate``() =
    bars.ClosingPrices().Length |> should equal 505
    bars.ClosingPrices()[0] |> should equal 75.08m
    bars.ClosingPrices()[504] |> should equal 49.14m
    
[<Fact>]
let ``Volumes accurate`` () =
    bars.Volumes().Length |> should equal 505
    bars.Volumes()[0] |> should equal 17157618m
    bars.Volumes()[504] |> should equal 6933219m
    
[<Fact>]
let ``Find by date returns none when not found``() =
    let result = bars.TryFindByDate(System.DateTimeOffset.Parse("2020-01-01"))
    result |> should equal None
    
[<Fact>]
let ``Find by date returns some when found``() =
    let result = bars.TryFindByDate(System.DateTimeOffset.Parse("2020-11-30"))
    result |> should equal (Some(bar, 0))