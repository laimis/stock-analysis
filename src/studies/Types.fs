namespace studies.Types

open System
open FSharp.Data
open core.fs.Adapters.Stocks

module Constants =
    [<Literal>]
    let MinimumRecords = 1_000

    [<Literal>]
    let EarliestYear = 2020

    [<Literal>]
    let LatestYear = 2024

type Signal =
    CsvProvider<
        Sample = "screenerid (int), date (string), ticker (string)",
        HasHeaders=true
    >

type SignalWithPriceProperties =
    CsvProvider<
        Schema = "screenerid (int), date (string), ticker (string), gap (decimal), sma20 (decimal), sma50 (decimal), sma150 (decimal), sma200 (decimal)",
        HasHeaders=false
    >

type TradeOutcomeOutput =
    CsvProvider<
        Sample = "screenerid (int), date (string), ticker (string), gap (decimal), sma20 (decimal), sma50 (decimal), sma150 (decimal), sma200 (decimal), strategy (string), longOrShort (string), opened (string), openPrice (decimal), closed (string), closePrice (decimal), percentGain (decimal), numberOfDaysHeld (int)",
        HasHeaders=true
    >

module Signal =
    let verifyRecords (input:Signal) minimumRecordsExpected =
        let records = input.Rows
        // make sure there is at least some records in here, ideally in thousands
        let numberOfRecords = records |> Seq.length
        match numberOfRecords with
        | x when x < minimumRecordsExpected -> failwith $"{x} is not enough records, expecting at least {minimumRecordsExpected}"
        | _ -> ()

        let verifyCondition failureConditionFunc messageIfFound =
            records
            |> Seq.map (fun r -> match failureConditionFunc r with | true -> Some r | false -> None)
            |> Seq.choose id
            |> Seq.tryHead
            |> Option.iter (fun r -> r |> messageIfFound |> failwith)

        // make sure all the dates are set (and can be parsed?)
        let invalidDate = fun (r:Signal.Row) -> match DateTimeOffset.TryParse(r.Date) with | true, _ -> false | false, _ -> true
        let messageIfInvalidDate = fun (r:Signal.Row) -> $"date is invalid for record {r.Screenerid}, {r.Ticker}"
        verifyCondition invalidDate messageIfInvalidDate

        let invalidTicker = fun (r:Signal.Row) -> String.IsNullOrWhiteSpace(r.Ticker)
        let messageIfInvalidTicker = fun (r:Signal.Row) -> $"ticker is blank for record {r.Screenerid}, {r.Date}"
        verifyCondition invalidTicker messageIfInvalidTicker

        let invalidScreenerId = fun (r:Signal.Row) -> r.Screenerid = 0
        let messageIfInvalidScreenerId = fun (r:Signal.Row) -> $"screenerid is blank for record {r.Ticker}, {r.Date}"
        verifyCondition invalidScreenerId messageIfInvalidScreenerId

        records

type Unified =
    | Input of Signal.Row
    | Output of SignalWithPriceProperties.Row

module Unified =
    let describeRecords (records:Unified seq) =

        let getDate unified =
            match unified with
            | Input row -> row.Date
            | Output row -> row.Date

        let getTicker unified =
            match unified with
            | Input row -> row.Ticker
            | Output row -> row.Ticker

        let getScreenerId unified =
            match unified with
            | Input row -> row.Screenerid
            | Output row -> row.Screenerid

        let numberOfRecords = records |> Seq.length

        match numberOfRecords with
        | x when x > 0 ->
            let dates = records |> Seq.map (fun r -> r |> getDate) |> Seq.distinct |> Seq.length
            let tickers = records |> Seq.map (fun r -> r |> getTicker) |> Seq.distinct |> Seq.length
            let screenerIds = records |> Seq.map (fun r -> r |> getScreenerId) |> Seq.distinct |> Seq.length

            let minimumDate = records |> Seq.minBy (fun r -> r |> getDate) |> getDate
            let maximumDate = records |> Seq.maxBy (fun r -> r |> getDate) |> getDate

            printfn $"Records: %d{numberOfRecords}, dates: %d{dates}, tickers: %d{tickers}, screenerIds: %d{screenerIds}"
            printfn $"Minimum date: %A{minimumDate}"
            printfn $"Maximum date: %A{maximumDate}"
            printfn ""
        | _ ->
            printfn $"No records found in the input"

module TradeOutcomeOutput =

    let create strategy positionType (signal:SignalWithPriceProperties.Row) (openBar:PriceBar) (closeBar:PriceBar) =

        let openPrice = openBar.Open
        let closePrice = closeBar.Close

        let daysHeld = closeBar.Date - openBar.Date

        let longOrShortString, gainFactor =
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> "long", 1m
            | core.fs.Stocks.StockPositionType.Short -> "short", -1m

        // calculate gain percentage
        let gain = (closePrice - openPrice) / openPrice * gainFactor

        TradeOutcomeOutput.Row(
            screenerid=signal.Screenerid,
            date=signal.Date,
            ticker=signal.Ticker,
            gap=signal.Gap,
            sma20=signal.Sma20,
            sma50=signal.Sma50,
            sma150=signal.Sma150,
            sma200=signal.Sma200,
            strategy=strategy,
            longOrShort=longOrShortString,
            opened=openBar.DateStr,
            openPrice=openPrice,
            closed=closeBar.DateStr,
            closePrice=closePrice,
            percentGain=gain,
            numberOfDaysHeld=(daysHeld.TotalDays |> int)
        )

type TradeSummary = {
    StrategyName:string
    NumberOfTrades:int
    Winners:int
    Losers:int
    TotalGain:decimal
    WinPct:decimal
    AvgWin:decimal
    AvgLoss:decimal
    AvgGainLoss:decimal
    EV:decimal
    Gains:decimal seq
    GainDistribution:core.fs.Services.Analysis.DistributionStatistics
}

module TradeSummary =
    let create name (outcomes:TradeOutcomeOutput.Row seq) =
        // summarize the outcomes
        let numberOfTrades = outcomes |> Seq.length
        let winners = outcomes |> Seq.filter (fun o -> o.PercentGain > 0m)
        let losers = outcomes |> Seq.filter (fun o -> o.PercentGain < 0m)
        let numberOfWinners = winners |> Seq.length
        let numberOfLosers = losers |> Seq.length
        let win_pct = decimal numberOfWinners / decimal numberOfTrades
        let avg_win =
            match numberOfWinners with
            | 0 -> 0m
            | _ -> winners |> Seq.averageBy (_.PercentGain)
        let avg_loss =
            match numberOfLosers with
            | 0 -> 0m
            | _ -> losers |> Seq.averageBy (_.PercentGain)
        let avg_gain_loss =
            match avg_loss with
            | 0m -> 0m
            | _ -> avg_win / avg_loss |> Math.Abs
        let ev = win_pct * avg_win - (1m - win_pct) * (avg_loss |> Math.Abs)
        let totalGain = outcomes |> Seq.sumBy (_.PercentGain)

        let gains = outcomes |> Seq.map (_.PercentGain)
        let gainDistribution = core.fs.Services.Analysis.DistributionStatistics.calculate gains

        // return trade summary
        {
            StrategyName = name
            NumberOfTrades = numberOfTrades
            Winners = numberOfWinners
            Losers = numberOfLosers
            TotalGain = totalGain
            WinPct = win_pct
            AvgWin = avg_win
            AvgLoss = avg_loss
            AvgGainLoss = avg_gain_loss
            EV = ev
            Gains = gains
            GainDistribution = gainDistribution
        }
