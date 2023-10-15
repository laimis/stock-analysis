namespace core.fs.Services.Analysis

open System.Collections.Generic
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

module OutcomeType =
    let Positive = "Positive"
    let Negative = "Negative"
    let Neutral = "Neutral"

type AnalysisOutcome(key:string, outcomeType:string, value:decimal, valueType:string, message:string) =
    member val Key = key
    member val OutcomeType = outcomeType
    member val Value = value
    member val ValueType = valueType
    member val Message = message
        
type TickerOutcomes =
    {
        outcomes: seq<AnalysisOutcome>
        ticker:string
    }
    
type AnalysisOutcomeEvaluation(name:string,``type``:string,sortColumn:string,matchingTickers:seq<TickerOutcomes>) =
    member val Name = name
    member val Type = ``type``
    member val SortColumn = sortColumn
    member val MatchingTickers = matchingTickers

type Pattern = 
    {
        date: System.DateTimeOffset
        name: string
        description: string
        value: decimal
        valueFormat: string
    }
    
type TickerPatterns =
    {
        patterns: seq<Pattern>
        ticker:string
    }
    
type SMA(values,interval) =
    
    member this.Values = values
    member this.Interval = interval
    member this.Description = $"SMA {interval}"
    member this.LastValue =
        match values |> Seq.isEmpty with
        | true -> None
        | false -> Some (values |> Seq.last)
        
    static member ToSMA (prices:decimal array) interval =
        
        let sma = Array.create prices.Length None
        
        for i in 0..prices.Length-1 do
            if i < interval then
                sma[i] <- None
            else
                let sum = 
                    [interval-1..-1..i-interval]
                    |> Seq.map (fun j -> prices[j])
                    |> Seq.sum
                    
                sma[i] <- Some (sum / decimal interval)
        
        SMA(sma, interval)        
        
type SMAContainer(sma20,sma50,sma150,sma200) =
    
    let all = [|sma20;sma50;sma150;sma200|]
        
    member this.All = all
    member this.Length = all.Length
    member this.sma20 = sma20
    member this.sma50 = sma50
    member this.sma150 = sma150
    member this.sma200 = sma200
    
    static member Generate (prices:PriceBar array) =
        
        SMAContainer(
            SMA.ToSMA (prices |> Array.map (fun p -> p.Close)) 20,
            SMA.ToSMA (prices |> Array.map (fun p -> p.Close)) 50,
            SMA.ToSMA (prices |> Array.map (fun p -> p.Close)) 150,
            SMA.ToSMA (prices |> Array.map (fun p -> p.Close)) 200
        )
    
module AnalysisOutcomeEvaluationScoringHelper =
    
    let generateTickerCounts evaluations =
        let counts = Dictionary<string, int>()
        
        evaluations
        |> Seq.iter (fun (category:AnalysisOutcomeEvaluation) ->
            let toAdd =
                // TODO: can this be union type
                match category.Type with
                | nameof OutcomeType.Positive -> 1
                | nameof OutcomeType.Negative -> -1
                | _ -> 0
                
            category.MatchingTickers
            |> Seq.iter (fun o ->
                if not (counts.ContainsKey(o.ticker)) then
                    counts[o.ticker] <- 0

                counts[o.ticker] <- counts[o.ticker] + toAdd
            )
        )
        
        counts

    let GenerateEvaluationCounts (evaluations:seq<AnalysisOutcomeEvaluation>) =
        let counts = Dictionary<string, int>()
        evaluations
        |> Seq.iter (fun category ->
            let key = category.Name

            if not (counts.ContainsKey(key)) then
                counts[key] <- 0

            counts[key] <- counts[key] + (category.MatchingTickers |> Seq.length)
        )
        counts

    
type DistributionStatistics =
    {
        count: decimal
        kurtosis: decimal
        min: decimal
        max: decimal
        mean: decimal
        median: decimal
        skewness: decimal
        stdDev: decimal
        buckets: array<ValueWithFrequency>
    }