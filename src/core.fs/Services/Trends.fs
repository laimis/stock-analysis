module core.fs.Services.Trends

open System.Threading
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis

type TrendDirection =
    | Up
    | Down
    
    static member FromString s =
        match s with
        | nameof Up -> Up
        | nameof Down -> Down
        | _ -> failwith $"Invalid trend type {s}"
        
    override this.ToString() =
        match this with
        | Up -> nameof Up
        | Down -> nameof Down
        
type TrendType =
    | Ema20OverSma50
    | Sma50OverSma200
    
    static member FromString s =
        match s with
        | nameof Ema20OverSma50 -> Ema20OverSma50
        | nameof Sma50OverSma200 -> Sma50OverSma200
        | _ -> failwith $"Invalid trend type {s}"
        
    override this.ToString() =
        match this with
        | Ema20OverSma50 -> nameof Ema20OverSma50
        | Sma50OverSma200 -> nameof Sma50OverSma200
    
type Trend = {
    ticker: Ticker
    start: PriceBarWithIndex
    end_: PriceBarWithIndex
    max: PriceBarWithIndex
    min: PriceBarWithIndex
    direction: TrendDirection
    trendType: TrendType
}
    with
        member this.NumberOfDays =
            let end_ = this.end_ |> snd
            let start = this.start |> snd
            (end_.Date - start.Date).TotalDays |> int
        member this.NumberOfBars =
            let end_ = this.end_ |> fst
            let start = this.start |> fst
            end_ - start
            
        member this.StartValue = this.start |> snd |> _.Close
        member this.StartDateStr = this.start |> snd |> _.DateStr
        member this.EndValue = this.end_ |> snd |> _.Close
        member this.EndDateStr = this.end_ |> snd |> _.DateStr
        member this.GainPercent = (this.EndValue - this.StartValue) / this.StartValue
        member this.MaxDate = this.max |> snd |> _.DateStr
        member this.MaxValue = this.max |> snd |> _.Close
        member this.MaxAge = (this.end_ |> fst) - (this.max |> fst)
        member this.MinDate = this.min |> snd |> _.DateStr
        member this.MinValue = this.min |> snd |> _.Close
        member this.MinAge = (this.end_ |> fst) - (this.min |> fst)
        
type Trends(ticker, trendType, trends:Trend list) =
    
    let compareByNumberOfBars = fun (t:Trend) -> t.NumberOfBars
    let compareByGain = fun (t:Trend) -> t.GainPercent
    
    let upTrends = trends |> List.filter (fun t -> t.direction = Up)
    let downTrends = trends |> List.filter (fun t -> t.direction = Down)
    
    let determineRank rankFunc trend =
        let matchingTrends = 
            match trend.direction with
            | Up -> upTrends
            | Down -> downTrends
            
        let rank = matchingTrends |> List.sortByDescending rankFunc |> List.findIndex (fun t -> t = trend) |> fun x -> x + 1
        rank, matchingTrends.Length
    
    member this.Trends = trends
    member this.UpTrends = upTrends
    member this.DownTrends = downTrends
    member this.CurrentTrend = trends |> List.last
    member this.Length = trends.Length
    member this.BarStatistics = trends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.UpBarStatistics = this.UpTrends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.DownBarStatistics = this.DownTrends |> List.map (fun t -> t.NumberOfBars |> decimal) |> DistributionStatistics.calculate
    member this.GainStatistics = trends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.UpGainStatistics = this.UpTrends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.DownGainStatistics = this.DownTrends |> List.map (_.GainPercent) |> DistributionStatistics.calculate
    member this.TrendType = trendType
    member this.Ticker = ticker
    member this.StartDateStr = trends |> List.head |> fun t -> t.start |> snd |> _.DateStr
    member this.EndDateStr = this.CurrentTrend.end_ |> snd |> _.DateStr
    
    member this.BarRank trend = determineRank compareByNumberOfBars trend
    member this.GainRank trend = determineRank compareByGain trend
    member this.CurrentTrendRankByBars = this.BarRank this.CurrentTrend |> fst
    member this.CurrentTrendRankByGain = this.GainRank this.CurrentTrend |> fst
    
module TrendCalculator =
    
    let private trendMASelection trendType (movingAverages:MovingAveragesContainer) =
        match trendType with
        | Ema20OverSma50 -> movingAverages.ema20, movingAverages.sma50
        | Sma50OverSma200 -> movingAverages.sma50, movingAverages.sma200
    
    let generate ticker trendType (prices:PriceBars) =
        
        let firstMas,secondMas = prices |> MovingAveragesContainer.Generate |> trendMASelection trendType
        
        let indexedValues =
            Array.mapi2 (fun i firstMa secondMa -> i, firstMa, secondMa) firstMas.Values secondMas.Values
            |> Array.filter (fun (_,firstMa,secondMa) -> firstMa.IsSome && secondMa.IsSome)
            |> Array.map (fun (i,firstMa,secondMa) -> i, firstMa.Value, secondMa.Value)
            
        let createTrend foundLocation direction trendType =
            let barWithIndex = prices.BarsWithIndex[foundLocation]
            
            {
                ticker = ticker
                start = barWithIndex
                end_ = barWithIndex
                max = barWithIndex
                min = barWithIndex
                direction = direction
                trendType = trendType
            }
            
        let initialDirection =
            match indexedValues[0] with
            | _, first, second ->
                match first > second with
                | true -> Up
                | false -> Down
                
        let initialTrend = createTrend 0 initialDirection trendType
            
        let latestTrendAndTrends =
            indexedValues
            |> Array.fold (fun (trend, trends) (i, firstMa, secondMa) ->
                
                let direction =
                    match firstMa > secondMa with
                    | true -> Up
                    | false -> Down
                
                match direction = trend.direction with
                | true ->
                    // check the max and see it needs to be updated
                    let max =
                        match prices.BarsWithIndex[i] with
                        | currentIndexWithBar when (currentIndexWithBar |> snd).Close > (trend.max |> snd).Close -> currentIndexWithBar
                        | _ -> trend.max
                        
                    // check the min and see it needs to be updated
                    let min =
                        match prices.BarsWithIndex[i] with
                        | currentIndexWithBar when (currentIndexWithBar |> snd).Close < (trend.min |> snd).Close -> currentIndexWithBar
                        | _ -> trend.min
                    
                    let newTrend = { trend with end_ = prices.BarsWithIndex[i]; max = max; min = min }
                    newTrend, trends
                | false ->
                    let newTrends = trends @ [trend]
                    let newTrend = createTrend i direction trendType
                    newTrend, newTrends
            ) (initialTrend, [])
            
        let trend, trends = latestTrendAndTrends
        
        // finish the last trend and add it to trends
        let trend = { trend with end_ = prices.BarsWithIndex[prices.BarsWithIndex.Length - 1] }
        
        Trends(ticker, trendType, trends @ [trend])
