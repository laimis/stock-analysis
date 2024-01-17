namespace studies.Types

open FSharp.Data

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
        let dates = records |> Seq.map (fun r -> r |> getDate) |> Seq.distinct |> Seq.length
        let tickers = records |> Seq.map (fun r -> r |> getTicker) |> Seq.distinct |> Seq.length
        let screenerIds = records |> Seq.map (fun r -> r |> getScreenerId) |> Seq.distinct |> Seq.length
        
        let minimumDate = records |> Seq.minBy (fun r -> r |> getDate) |> getDate
        let maximumDate = records |> Seq.maxBy (fun r -> r |> getDate) |> getDate
        
        printfn $"Records: %d{numberOfRecords}, dates: %d{dates}, tickers: %d{tickers}, screenerIds: %d{screenerIds}"
        printfn $"Minimum date: %A{minimumDate}"
        printfn $"Maximum date: %A{maximumDate}"
        printfn ""
    
type TradeOutcomeOutput =
    CsvProvider<
        Sample = "strategy (string), ticker (string), date (string), screenerid (int), gap (decimal), opened (string), openPrice (decimal), closed (string), closePrice (decimal), percentGain (decimal), numberOfDaysHeld (int)",
        HasHeaders=true
    >
    
module TradeOutcomeOutput =
    let save (filepath:string) outcomes =
        let csv = new TradeOutcomeOutput(outcomes)
        csv.Save(filepath)
        
    let parseTradeOutcomes (filepath:string) =
        TradeOutcomeOutput.Load(filepath).Rows

type TradeSummary = {
    StrategyName:string
    Total:int
    Winners:int
    Losers:int
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
        let total = outcomes |> Seq.length
        let winners = outcomes |> Seq.filter (fun o -> o.PercentGain > 0m)
        let losers = outcomes |> Seq.filter (fun o -> o.PercentGain < 0m)
        let numberOfWinners = winners |> Seq.length
        let numberOfLosers = losers |> Seq.length
        let win_pct = decimal numberOfWinners / decimal total
        let avg_win = winners |> Seq.averageBy (fun o -> o.PercentGain)
        let avg_loss = losers |> Seq.averageBy (fun o -> o.PercentGain)
        let avg_gain_loss = avg_win / avg_loss |> System.Math.Abs
        let ev = win_pct * avg_win - (1m - win_pct) * (avg_loss |> System.Math.Abs)

        let gains = outcomes |> Seq.map (fun o -> o.PercentGain)
        let gainDistribution = core.fs.Services.Analysis.DistributionStatistics.calculate gains
        
        // return trade summary 
        {
            StrategyName = name
            Total = total
            Winners = numberOfWinners
            Losers = numberOfLosers
            WinPct = win_pct
            AvgWin = avg_win
            AvgLoss = avg_loss
            AvgGainLoss = avg_gain_loss
            EV = ev
            Gains = gains
            GainDistribution = gainDistribution 
        }