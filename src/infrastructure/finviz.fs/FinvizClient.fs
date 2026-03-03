namespace finviz

module FinvizClient =

    let mutable private outputFunc: string -> unit = fun _ -> ()

    let private SLEEP_BETWEEN_REQUESTS_MS = 250

    let setOutputFunc (f: string -> unit) =
        outputFunc <- f
        FinvizParsing.setOutputFunc f

    let private cleanString (value: string) =
        let nonAlpha = System.Text.RegularExpressions.Regex "[^a-zA-Z]"
        nonAlpha.Replace(value, "").ToLower()

    let private fetchUrl (url: string) =
        System.Threading.Thread.Sleep SLEEP_BETWEEN_REQUESTS_MS
        outputFunc url
        let web = HtmlAgilityPack.HtmlWeb()
        web.Load url

    let getResults url =
        let rec fetchPage offset (results: ScreenerResult list) =
            let urlToFetch = url + "&r=" + string offset
            let htmlDoc = fetchUrl urlToFetch

            outputFunc urlToFetch

            let page =
                htmlDoc
                |> FinvizParsing.parseScreenerHtml
                |> Seq.toList

            // check if the page has only one element and offset is greater than one
            // this potentially could be a repeated result; finviz just returns the last result
            // whenever a new page is requested. When total results is something like 20 or 40
            // we don't know that this is the end and just keep fetching
            let isLastPage = page.Length = 1 && offset > 1
            let index = results |> List.tryFindIndex (fun r -> page.Length > 0 && r.ticker.Equals page[0].ticker)

            if index.IsSome && isLastPage then
                results
            else
                match page with
                | c when c.Length = 20 -> fetchPage (offset + 20) (List.append results page)
                | _ -> List.append results page

        fetchPage 1 []

    let getResultCount url =
        url |> fetchUrl |> FinvizParsing.parseResultCount

    let getResultCountForIndustryAboveAndBelowSMA (smaDays: int) (industry: string) =
        let cleaned = industry |> cleanString

        let fetchCountWithTA ta =
            let url = $"https://finviz.com/screener.ashx?v=111&f=ind_{cleaned},{ta}"
            url |> getResultCount

        let above = $"ta_sma%i{smaDays}_pa" |> fetchCountWithTA
        let below = $"ta_sma%i{smaDays}_pb" |> fetchCountWithTA

        above, below

    let getEarnings () =
        let before =
            getResults "https://finviz.com/screener.ashx?v=111&s=n_earningsbefore"
            |> List.map (fun r -> r.ticker, BeforeMarket)

        let after =
            getResults "https://finviz.com/screener.ashx?v=111&s=n_earningsafter"
            |> List.map (fun r -> r.ticker, AfterMarket)

        List.concat [ before; after ]

    let getResultCountForCountryAboveAndBelowSMA (smaDays: int) (country: string) =
        let cleaned = country |> cleanString

        let fetchCountWithTA ta =
            let url = $"https://finviz.com/screener.ashx?v=111&f=geo_{cleaned},{ta}"
            url |> getResultCount

        let above = $"ta_sma%i{smaDays}_pa" |> fetchCountWithTA
        let below = $"ta_sma%i{smaDays}_pb" |> fetchCountWithTA

        above, below
