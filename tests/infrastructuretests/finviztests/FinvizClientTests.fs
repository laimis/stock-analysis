namespace finviztests

open Xunit
open Xunit.Abstractions
open FsUnit

module FinvizClientTests =

    open finviz

    type ParsingTests(output: ITestOutputHelper) =

        do
            FinvizClient.setOutputFunc (fun str -> output.WriteLine str)

        let screenerUrl =
            "https://finviz.com/screener.ashx?v=111&f=cap_mega,sh_price_50to100&ft=4"

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``End to end fetch works``() =
            let results = screenerUrl |> FinvizClient.getResults

            results |> should not' (be Empty)

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``End to end earnings works``() =
            let earnings = FinvizClient.getEarnings ()

            earnings |> should not' (be Empty)

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``Fetch count works``() =
            let count = screenerUrl |> FinvizClient.getResultCount

            count |> should be (greaterThan 0)

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``Industry fetch works``() =
            let (above, below) =
                "Computer Hardware"
                |> FinvizClient.getResultCountForIndustryAboveAndBelowSMA 20

            above |> should be (greaterThan 0)
            below |> should be (greaterThan 0)

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``Industry with special characters works``() =
            let (above, below) =
                "Furnishings, Fixtures & Appliances"
                |> FinvizClient.getResultCountForIndustryAboveAndBelowSMA 20

            let total = above + below

            total |> should be (lessThan 100)

        [<Fact>]
        [<Trait("Category", "Integration")>]
        member _.``Country fetch works``() =
            let (above, below) =
                "USA"
                |> FinvizClient.getResultCountForCountryAboveAndBelowSMA 20

            above |> should be (greaterThan 0)
            below |> should be (greaterThan 0)
