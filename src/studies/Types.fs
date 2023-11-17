namespace studies.Types

open FSharp.Data

module Constants =
    [<Literal>]
    let MinimumRecords = 1_000
    
    [<Literal>]
    let EarliestYear = 2020
    
    [<Literal>]
    let LatestYear = 2024

type StudyInput =
    CsvProvider<
        Sample = "date (date), ticker (string), screenerid (int)",
        HasHeaders=true
    >

type GapStudyOutput =
    CsvProvider<
        Schema = "ticker (string), date (date), screenerid (int), hasGapUp (bool)",
        HasHeaders=false
    >
    
type TradeOutcomeOutput =
    CsvProvider<
        Sample = "strategy (string), ticker (string), date (date), screenerid (int), hasGapUp (bool), opened (date), openPrice (decimal), closed (date), closePrice (decimal), percentGain (decimal), numberOfDaysHeld (int)",
        HasHeaders=true
    >

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