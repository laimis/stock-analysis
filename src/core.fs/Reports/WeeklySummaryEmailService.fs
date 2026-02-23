namespace core.fs.Reports

open System
open System.Globalization
open core.fs
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Reports
open core.fs.Stocks

type WeeklySummaryEmailService(
    accounts: IAccountStorage,
    emails: IEmailService,
    reportsHandler: ReportsHandler,
    logger: ILogger) =

    let formatCurrency (value: decimal) =
        value.ToString("C2", CultureInfo.CreateSpecificCulture("en-US"))

    let formatPercent (value: decimal) =
        (value * 100m).ToString("F2", CultureInfo.InvariantCulture) + "%"

    let profitColor (value: decimal) =
        if value >= 0m then "#059669" else "#dc2626"

    let rowColor (value: decimal) =
        if value >= 0m then "#f0fdf4" else "#fef2f2"

    let formatDate (dt: DateTimeOffset option) =
        match dt with
        | Some d -> d.ToString("MM/dd/yyyy")
        | None -> ""

    let buildEmailPayload (view: WeeklySummaryView) =
        let closedStocks =
            view.ClosedStocks
            |> List.map (fun s ->
                {|
                    ticker = s.Ticker.Value
                    profit = formatCurrency s.Profit
                    profit_color = profitColor s.Profit
                    rr = s.RR.ToString("F2", CultureInfo.InvariantCulture)
                    gain_pct = formatPercent s.GainPct
                    grade = s.Grade |> Option.map (fun g -> g.Value) |> Option.defaultValue ""
                    row_color = rowColor s.Profit
                |}
            )
            |> List.toArray

        let openedStocks =
            view.OpenedStocks
            |> List.map (fun s ->
                {|
                    ticker = s.Ticker.Value
                    position_type = if s.IsShort then "SHORT" else "LONG"
                    shares = s.CompletedPositionShares.ToString("F0", CultureInfo.InvariantCulture)
                    cost = (s.CompletedPositionCostPerShare * s.CompletedPositionShares).ToString("F2", CultureInfo.InvariantCulture)
                    opened = s.Opened.ToString("MM/dd/yyyy")
                |}
            )
            |> List.toArray

        let plStockTransactions =
            view.PLStockTransactions
            |> List.map (fun t ->
                {|
                    ticker = t.Ticker.Value
                    shares = t.NumberOfShares.ToString("F0", CultureInfo.InvariantCulture)
                    profit = formatCurrency t.Profit
                    profit_color = profitColor t.Profit
                    date = t.Date.ToString("MM/dd/yyyy")
                |}
            )
            |> List.toArray

        let openedOptions =
            view.OpenedOptions
            |> List.map (fun o ->
                {|
                    ticker = o.UnderlyingTicker.Value
                    opened = formatDate o.Opened
                    cost = formatCurrency (o.Cost * 100m)
                |}
            )
            |> List.toArray

        let closedOptions =
            view.ClosedOptions
            |> List.map (fun o ->
                {|
                    ticker = o.UnderlyingTicker.Value
                    profit = formatCurrency (o.Profit * 100m)
                    profit_color = profitColor o.Profit
                    opened = formatDate o.Opened
                    closed = formatDate o.Closed
                |}
            )
            |> List.toArray

        let dividends =
            view.Dividends
            |> List.map (fun d ->
                {|
                    ticker = d.Ticker.Value
                    amount = formatCurrency d.NetAmount
                    date = d.Date.ToString("MM/dd/yyyy")
                    description = d.Description
                |}
            )
            |> List.toArray

        let fees =
            view.Fees
            |> List.map (fun f ->
                {|
                    ticker = f.Ticker.Value
                    amount = formatCurrency f.NetAmount
                    date = f.Date.ToString("MM/dd/yyyy")
                    description = f.Description
                |}
            )
            |> List.toArray

        {|
            start_date = view.Start.ToString("MM/dd/yyyy")
            end_date = view.End.ToString("MM/dd/yyyy")
            total_profit = formatCurrency view.TotalProfit
            total_profit_color = profitColor view.TotalProfit
            stock_profit = formatCurrency view.StockProfit
            stock_profit_color = profitColor view.StockProfit
            option_profit = formatCurrency view.OptionProfit
            option_profit_color = profitColor view.OptionProfit
            dividend_profit = formatCurrency view.DividendProfit
            dividend_profit_color = profitColor view.DividendProfit
            opened_stocks_count = view.OpenedStocks.Length
            closed_stocks_count = view.ClosedStocks.Length
            opened_options_count = view.OpenedOptions.Length
            closed_options_count = view.ClosedOptions.Length
            closed_stocks = closedStocks
            opened_stocks = openedStocks
            pl_stock_transactions = plStockTransactions
            opened_options = openedOptions
            closed_options = closedOptions
            dividends = dividends
            fees = fees
        |}

    interface IApplicationService

    member _.Execute() = task {
        logger.LogInformation("Starting weekly summary email job")

        let! pairs = accounts.GetUserEmailIdPairs()

        let! _ =
            pairs
            |> Seq.map (fun pair -> async {
                try
                    let! userResult = pair.Id |> accounts.GetUser |> Async.AwaitTask
                    match userResult with
                    | None ->
                        logger.LogWarning($"User not found for id {pair.Id}, skipping weekly summary email")
                    | Some user ->
                        let query = {
                            WeeklySummaryQuery.Period = "last7days"
                            UserId = pair.Id
                        }

                        let! view = reportsHandler.Handle(query) |> Async.AwaitTask
                        let payload = buildEmailPayload view

                        let recipient = Recipient(user.State.Email, user.State.Name)
                        let sender = Sender.NoReply

                        let! result = emails.SendWeeklySummary recipient sender payload |> Async.AwaitTask
                        match result with
                        | Ok () ->
                            logger.LogInformation($"Weekly summary email sent to {user.State.Email}")
                        | Error err ->
                            logger.LogError($"Failed to send weekly summary email to {user.State.Email}: {err}")
                with ex ->
                    logger.LogError($"Error sending weekly summary email to user {pair.Email}: {ex.Message}")
            })
            |> Async.Sequential

        logger.LogInformation("Completed weekly summary email job")
        return ()
    }
