module core.fs.Services.PendingPositionAnalysis

open System
open core.Stocks
open core.fs
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis

module PendingPositionAnalysisKeys =
    let Price = "Price"
    let Bid = "Bid"
    let PercentFromPrice = "PercentFromPrice"
    let PositionSize = "PositionSize"
    let StopPrice = "StopPrice"
    let RiskedAmount = "RiskedAmount"
    let NumberOfDaysPending = "NumberOfDaysPending"
    
let generate (position:PendingStockPositionState) (bars:PriceBars) =
        
    let price = bars.Last.Close
    
    let percentFromPrice = position.Bid / price - 1m
    
    let positionSize = position.Bid * position.NumberOfShares
    
    let riskedAmount = (position.Bid - (position.StopPrice |> Option.defaultValue 0m)) * position.NumberOfShares
    
    let numberOfDaysPending = (DateTimeOffset.Now - position.Created).TotalDays |> int
    
    {
        ticker = position.Ticker
        outcomes =
            [
                AnalysisOutcome(PendingPositionAnalysisKeys.Price, OutcomeType.Neutral, price, ValueFormat.Currency, $"$Price is {price}")
                AnalysisOutcome(PendingPositionAnalysisKeys.Bid, OutcomeType.Neutral, position.Bid, ValueFormat.Currency, $"Bid is {position.Bid}")
                AnalysisOutcome(PendingPositionAnalysisKeys.PercentFromPrice, OutcomeType.Neutral, percentFromPrice, ValueFormat.Percentage, $"Difference between bid and price is {percentFromPrice:P2}")
                AnalysisOutcome(PendingPositionAnalysisKeys.PositionSize, OutcomeType.Neutral, positionSize, ValueFormat.Currency, $"Position size is {positionSize}")
                AnalysisOutcome(PendingPositionAnalysisKeys.StopPrice, OutcomeType.Neutral, position.StopPrice |> Option.defaultValue 0m, ValueFormat.Currency, $"Stop is {position.StopPrice |> Option.defaultValue 0m}")
                AnalysisOutcome(PendingPositionAnalysisKeys.RiskedAmount, OutcomeType.Neutral, riskedAmount, ValueFormat.Currency, $"Risked amount is {riskedAmount}")
                AnalysisOutcome(PendingPositionAnalysisKeys.NumberOfDaysPending, OutcomeType.Neutral, numberOfDaysPending, ValueFormat.Number, $"Number of days pending is {numberOfDaysPending}")
            ]
    }
    
let evaluate (tickerOutcomes:seq<TickerOutcomes>) =
    
    [
        AnalysisOutcomeEvaluation(
            "Close to execution",
            OutcomeType.Neutral,
            PendingPositionAnalysisKeys.PercentFromPrice,
            tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PendingPositionAnalysisKeys.PercentFromPrice && o.Value > 0.04m) ]
        )
        
        AnalysisOutcomeEvaluation(
            "Risk level too small",
            OutcomeType.Negative,
            PendingPositionAnalysisKeys.RiskedAmount,
            tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PendingPositionAnalysisKeys.RiskedAmount && o.Value < 20m) ]
            )
        
        AnalysisOutcomeEvaluation(
            "Pending for long time",
            OutcomeType.Negative,
            PendingPositionAnalysisKeys.NumberOfDaysPending,
            tickerOutcomes |> TickerOutcomes.filter [ (fun o -> o.Key = PendingPositionAnalysisKeys.NumberOfDaysPending && o.Value > 30m) ]
            )
    ]
