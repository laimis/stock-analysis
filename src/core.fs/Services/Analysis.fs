namespace core.fs.Services.Analysis

open System.Collections.Generic
open core.Shared
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

type OutcomeType =
    | Positive
    | Negative
    | Neutral
    
    with
        static member FromString(value:string) =
            match value with
            | nameof Positive -> Positive
            | nameof Negative -> Negative
            | nameof Neutral -> Neutral
            | _ -> failwith $"Unknown OutcomeType: {value}"
            
        override this.ToString() =
            match this with
            | Positive -> nameof Positive
            | Negative -> nameof Negative
            | Neutral -> nameof Neutral

type AnalysisOutcome(key:string, outcomeType:OutcomeType, value:decimal, valueType:ValueFormat, message:string) =
    member val Key = key
    member val OutcomeType = outcomeType
    member val Value = value
    member val ValueType = valueType
    member val Message = message
    
type TickerOutcomes =
    {
        outcomes: seq<AnalysisOutcome>
        ticker:Ticker
    }
    
module TickerOutcomes =
    let filter conditions (tickerOutcomes:TickerOutcomes seq) =
        tickerOutcomes
        |> Seq.filter (
            fun tickerOutcome ->
                conditions
                |> List.forall (fun condition ->
                    tickerOutcome.outcomes |> Seq.exists condition
                    )
                )
    
type AnalysisOutcomeEvaluation(name:string,``type``:OutcomeType,sortColumn:string,matchingTickers:seq<TickerOutcomes>) =
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
        valueFormat: ValueFormat
    }
    
type TickerPatterns =
    {
        patterns: seq<Pattern>
        ticker:Ticker
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
                    [i-interval..i-1]
                    |> Seq.map (fun j -> prices[j])
                    |> Seq.sum
                    
                sma[i] <- Some (System.Math.Round(sum / decimal interval, 2))
        
        SMA(sma, interval)        
        
type SMAContainer(all:SMA array) =
    
    member this.All = all
    member this.Length = all.Length
    member this.sma20 = all |> Array.find (fun x -> x.Interval = 20)
    member this.sma50 = all |> Array.find (fun x -> x.Interval = 50)
    member this.sma150 = all |> Array.find (fun x -> x.Interval = 150)
    member this.sma200 = all |> Array.find (fun x -> x.Interval = 200)
    
    static member Generate (prices:PriceBars) =
        
        let closingPrices = prices.ClosingPrices()
        
        let generate = closingPrices |> SMA.ToSMA
        
        [|20;50;150;200|] |> Array.map generate |> SMAContainer
    
module AnalysisOutcomeEvaluationScoringHelper =
    
    let generateTickerCounts evaluations =
        let counts = Dictionary<Ticker, int>()
        
        evaluations
        |> Seq.iter (fun (category:AnalysisOutcomeEvaluation) ->
            let toAdd =
                match category.Type with
                | Positive -> 1
                | Negative -> -1
                | Neutral -> 0
                
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

module Histogram =
    // by default number of buckets should be 21
    let calculate (numbers:decimal array) (min:decimal) max numberOfBuckets : array<ValueWithFrequency> =
        
        let bucketSize = (max - min) / decimal(numberOfBuckets)
        
        // if the bucket size is really small, use just two decimal places
        // otherwise use 0 decimal places
        let bucketSize = 
            if bucketSize < 1m then
                System.Math.Round(bucketSize, 4)
            else
                System.Math.Floor(bucketSize)
        
        let min = 
            if bucketSize < 1m then
                System.Math.Round(min, 4)
            else
                System.Math.Floor(min)
        
        let result =
            List<ValueWithFrequency>(
                [0 .. numberOfBuckets - 1]
                |> List.map (fun i -> {value = min + (decimal(i) * bucketSize); frequency = 0})
            )
        numbers
        |> Array.iter ( fun n ->
            if n > result[result.Count - 1].value then
                result[result.Count - 1] <- {value = result[result.Count - 1].value; frequency = result[result.Count - 1].frequency + 1}
            else
                let firstSlot = result |> Seq.findIndex (fun x -> x.value >= n)
                result[firstSlot] <- {value = result[firstSlot].value; frequency = result[firstSlot].frequency + 1}
        )
        
        result.ToArray()
    
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
    
    with
    
        static member calculate (numbers:decimal array) =
           
            if numbers.Length = 0 then
                {
                    count = 0m
                    kurtosis = 0m
                    min = 0m
                    max = 0m
                    mean = 0m
                    median = 0m
                    skewness = 0m
                    stdDev = 0m
                    buckets = [||] 
                }
            else
                let mean = System.Math.Round(numbers |> Array.average, 2)
                let min = System.Math.Round(numbers |> Array.min, 2)
                let max = System.Math.Round(numbers |> Array.max, 2)
                
                let median =
                    System.Math.Round(
                    numbers
                    |> Array.sort
                    |> Array.skip (numbers.Length / 2)
                    |> Array.head,
                    2)
                
                let count = numbers.Length
                
                let stdDevDouble =
                    System.Math.Round(
                    numbers
                    |> Array.map (fun x -> System.Math.Pow(double(x - mean), 2))
                    |> Array.sum
                    |> fun x -> x / ((numbers.Length - 1) |> float)
                    |> System.Math.Sqrt,
                    2)
                
                let stdDev = 
                    match stdDevDouble with
                    | double.PositiveInfinity -> 0m
                    | double.NegativeInfinity -> 0m
                    | _ -> decimal stdDevDouble
                
                let skewnessDouble =
                    numbers
                    |> Array.map (fun x -> System.Math.Pow(double(x - mean), 3))
                    |> Array.sum
                    |> fun x -> x / double(count) / System.Math.Pow(double stdDev, 3)
                    
                
                let skewness = 
                    match skewnessDouble with
                    | double.PositiveInfinity -> 0m
                    | double.NegativeInfinity -> 0m
                    | _ -> decimal skewnessDouble
                
                let kurtosisDouble = 
                    numbers
                    |> Array.map (fun x -> System.Math.Pow(double(x - mean), 4))
                    |> Array.sum
                    |> fun x -> x / double(count) / System.Math.Pow(double stdDev, 4) - 3.0
                
                let kurtosis = 
                    match kurtosisDouble with
                    | double.PositiveInfinity -> 0m
                    | double.NegativeInfinity -> 0m
                    | _ -> decimal kurtosisDouble
                    
                let buckets = Histogram.calculate numbers min max 21
                
                {
                    count = decimal count
                    kurtosis = kurtosis
                    min = min
                    max = max
                    mean = mean
                    median = median
                    skewness = skewness
                    stdDev = stdDev
                    buckets = buckets
                }
                
module PercentChangeAnalysis =
    
    let calculate multipleByHundred numbers =
        
        let percentChanges = 
            numbers 
            |> Seq.pairwise 
            |> Seq.map (fun (x, y) ->
                let change =
                    match x with
                    | 0m -> 0m
                    | _ -> (y - x) / x
                match multipleByHundred with
                | true -> System.Math.Round(change * 100m, 2)
                | false -> change
            )
            |> Seq.toArray

        DistributionStatistics.calculate percentChanges
        
    let calculateForPriceBars (priceBars:PriceBars) =
        priceBars.ClosingPrices() |> calculate true