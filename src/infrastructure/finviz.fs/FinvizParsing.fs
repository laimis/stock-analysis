namespace finviz

module FinvizParsing =

    let mutable private outputFunc: string -> unit = fun _ -> ()

    let setOutputFunc (f: string -> unit) =
        outputFunc <- f

    let parseResultCount (doc: HtmlAgilityPack.HtmlDocument) =
        let nodes =
            doc.DocumentNode.SelectNodes "//table[@id='screener-views-table']/tr"
            |> Seq.toList

        let nodesContainingTotal = nodes.Item(2).SelectNodes "//div[@id='screener-total']"

        let totalText =
            match nodesContainingTotal with
            | null ->
                nodes.Item(2).SelectNodes("//td[@class='count-text']").Item(0).InnerText
            | _ ->
                nodesContainingTotal.Item(0).InnerText

        outputFunc totalText

        let removeTotalMarker (input: string) =
            input.Replace("Total", "")

        match totalText with
        | x when x.Contains "#" ->
            let total = x.Substring(x.IndexOf "/" + 1)
            System.Int32.Parse(total |> removeTotalMarker)
        | _ ->
            System.Int32.Parse(totalText |> removeTotalMarker)

    let parseScreenerHtml (doc: HtmlAgilityPack.HtmlDocument) =

        let extractValueFromScreenerCell (node: HtmlAgilityPack.HtmlNode) =
            node.ChildNodes[0].InnerText

        let (|Decimal|_|) str =
            match System.Decimal.TryParse(str: string) with
            | true, dec -> Some dec
            | _ -> None

        let processScreenerRow (node: HtmlAgilityPack.HtmlNode) : ScreenerResult option =
            let toDecimal str =
                match str with
                | Decimal dec -> dec
                | _ -> raise (System.Exception("toDecimal conversion failed for " + str))

            let fromCapToDecimal (value: string) =
                match value with
                | "-" -> 0m
                | _ ->
                    let lastChar = value[value.Length - 1]

                    let numericPortion =
                        match value.Substring(0, value.Length - 1) with
                        | Decimal dec -> dec
                        | _ -> raise (System.Exception("fromCap numeric conversion failed for " + value))

                    match lastChar with
                    | 'M' -> numericPortion * 1_000_000m
                    | 'B' -> numericPortion * 1_000_000_000m
                    | _ -> raise (System.Exception("Cap to decimal conversion failed for " + value))

            let toInt str =
                try
                    System.Int64.Parse str
                with _ ->
                    raise (System.Exception("toInt conversion failed for " + str))

            let remove characterToRemove str =
                String.filter (fun c -> c.Equals characterToRemove |> not) str

            match node.ChildNodes.Count with
            | 0 -> None
            | _ ->
                let ticker = node.ChildNodes[2] |> extractValueFromScreenerCell
                let company = extractValueFromScreenerCell node.ChildNodes[3]
                let sector = extractValueFromScreenerCell node.ChildNodes[4]
                let industry = extractValueFromScreenerCell node.ChildNodes[5]
                let country = extractValueFromScreenerCell node.ChildNodes[6]
                let marketCap = extractValueFromScreenerCell node.ChildNodes[7] |> fromCapToDecimal
                let price = extractValueFromScreenerCell node.ChildNodes[9] |> toDecimal
                let change = extractValueFromScreenerCell node.ChildNodes[10] |> remove '%' |> toDecimal
                let volume = extractValueFromScreenerCell node.ChildNodes[11] |> remove ',' |> toInt

                Some {
                    ticker = ticker.ToUpper()
                    company = company
                    sector = sector
                    industry = industry
                    country = country
                    marketCap = marketCap
                    price = price
                    change = change
                    volume = volume
                }

        // this code is very sensitive to changes on finviz side
        let nodes =
            doc.DocumentNode.SelectNodes
                "//table[@class='styled-table-new is-rounded is-tabular-nums w-full screener_table']/tr"

        match nodes with
        | null -> Seq.empty
        | _ ->
            nodes
            |> Seq.map processScreenerRow
            |> Seq.filter _.IsSome
            |> Seq.map _.Value
