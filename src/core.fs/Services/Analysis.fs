namespace core.fs.Services.Analysis

open System.Collections.Generic
open core.Shared
open core.fs
open core.fs.Adapters.Stocks

module Constants =
    let NumberOfDaysForRecentAnalysis = 60

type OutcomeType =
    | Positive
    | Negative
    | Neutral
    
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

type DailyPositionBreakdown =
    {
        DailyProfit: ChartDataPointContainer<decimal>
        DailyGainPct: ChartDataPointContainer<decimal>
        DailyObv: ChartDataPointContainer<decimal>
        DailyClose: ChartDataPointContainer<decimal>
    }

type Pattern = 
    {
        date: System.DateTimeOffset
        name: string
        description: string
        value: decimal
        valueFormat: ValueFormat
        sentimentType: SentimentType
    }
    
type TickerPatterns =
    {
        patterns: seq<Pattern>
        ticker:Ticker
    }
    
type MovingAverages(values,interval,exponential) =
    
    member this.Values = values
    member this.Interval = interval
    member this.Exponential = exponential
    member this.LastValue =
        match values |> Seq.isEmpty with
        | true -> None
        | false -> values |> Seq.last
        
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
        
        MovingAverages(sma, interval, false)
        
    static member toEMA (prices:decimal array) interval =
        
        let ema =
            match prices.Length <= interval with
            | true ->
                Array.create prices.Length None
            | false ->
                let alpha = 2m / (decimal (interval + 1))
                let initialEMA = System.Math.Round(Array.averageBy id prices[..(interval - 1)], 2)

                prices
                |> Array.skip interval
                |> Array.scan
                    (fun prevEMA price ->
                        let newEMA = alpha * price + (1m - alpha) * (prevEMA |> Option.get)
                        System.Math.Round(newEMA, 2) |> Some)
                    (initialEMA |> Some)
                |> Array.append (Array.create (interval-1) None) // need to subtract one because Array.scan includes the initial value
            
        MovingAverages(ema, interval, true)
        
type MovingAveragesContainer(all:MovingAverages array) =
    
    member this.All = all
    member this.Length = all.Length
    
    member this.ema20 = all |> Array.find (fun x -> x.Interval = 20 && x.Exponential = true)
    member this.sma20 = all |> Array.find (fun x -> x.Interval = 20 && x.Exponential = false)
    member this.sma50 = all |> Array.find (fun x -> x.Interval = 50 && x.Exponential = false)
    member this.sma150 = all |> Array.find (fun x -> x.Interval = 150 && x.Exponential = false)
    member this.sma200 = all |> Array.find (fun x -> x.Interval = 200 && x.Exponential = false)
    
    static member Generate (prices:decimal array) =
        let generateSMA = prices |> MovingAverages.ToSMA
        let generateEMA = prices |> MovingAverages.toEMA
        
        let sma = [|20;50;150;200|] |> Array.map generateSMA
        let ema = [|20;|] |> Array.map generateEMA
        
        MovingAveragesContainer(Array.append ema sma)
        
    static member Generate (prices:PriceBars) = prices.ClosingPrices() |> MovingAveragesContainer.Generate
    
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
    
    let private rounded (value:decimal) =
        System.Math.Round(value, 4)
        
    let calculate (min:decimal) max numberOfBuckets (numbers:decimal seq) : array<ValueWithFrequency> =
        
        let histogram = MathNet.Numerics.Statistics.Histogram(numbers |> Seq.map float, numberOfBuckets, min |> float, max |> float)
        
        [|0..histogram.BucketCount-1|]
        |> Array.map (fun i -> { value = histogram.Item(i).LowerBound |> decimal |> rounded; frequency = histogram.Item(i).Count |> int })
        
    let calculateFromSequence symmetric numberOfBuckets (numbers:decimal seq) =
        let min, max =
            numbers
            |> Seq.fold (fun (min,max) value ->
                let min = if value < min then value else min
                let max = if value > max then value else max
                (min,max)
            ) (System.Decimal.MaxValue, System.Decimal.MinValue)
        ()
    
        let min = System.Math.Floor(min)
        let max = System.Math.Ceiling(max)
        
        let min,max =
            match symmetric with
            | true ->
                let absMax = System.Math.Max(System.Math.Abs(min), System.Math.Abs(max))
                (-absMax, absMax)
            | false -> (min,max)
        
        calculate min max numberOfBuckets numbers
    
type DistributionStatistics =
    {
        count: int64
        kurtosis: decimal
        min: decimal
        max: decimal
        mean: decimal
        median: decimal
        skewness: decimal
        stdDev: decimal
        buckets: array<ValueWithFrequency>
    }
    
module DistributionStatistics =
    let calculate (numbers:decimal seq) =
        
        if numbers |> Seq.isEmpty then
            {
                count = 0
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
            let floats = numbers |> Seq.map float
        
            let stats = MathNet.Numerics.Statistics.DescriptiveStatistics(floats)
            let buckets = Histogram.calculateFromSequence false 21 numbers
            
            let kurtosis =
                match stats.Kurtosis with
                | x when System.Double.IsNaN(x) -> 0m
                | _ -> stats.Kurtosis |> decimal
                
            let skewness =
                match stats.Skewness with
                | x when System.Double.IsNaN(x) -> 0m
                | _ -> stats.Skewness |> decimal
                
            let stdDev =
                match stats.StandardDeviation with
                | x when System.Double.IsNaN(x) -> 0m
                | _ -> stats.StandardDeviation |> decimal
            
            {
                count = stats.Count
                kurtosis = kurtosis
                min = stats.Minimum |> decimal
                max = stats.Maximum |> decimal
                mean = stats.Mean |> decimal
                median = floats |> Seq.toArray |> Array.sort |> (fun a -> a[a.Length / 2]) |> decimal
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
