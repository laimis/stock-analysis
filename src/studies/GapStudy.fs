module studies.GapStudy

open System
open core.Shared
open core.fs.Services.GapAnalysis
open core.fs.Shared.Adapters.Stocks
open studies.Types

let summarize strategyName (outcomes:TradeOutcomeOutput.Row seq) =
    // summarize the outcomes
    let total = outcomes |> Seq.length
    let winners = outcomes |> Seq.filter (fun o -> o.PercentGain > 0m)
    let losers = outcomes |> Seq.filter (fun o -> o.PercentGain < 0m)
    let numberOfWinners = winners |> Seq.length
    let numberOfLosers = losers |> Seq.length
    let win_pct = decimal numberOfWinners / decimal total
    let avg_win = winners |> Seq.averageBy (fun o -> o.PercentGain)
    let avg_loss = losers |> Seq.averageBy (fun o -> o.PercentGain)
    let avg_gain_loss = avg_win / avg_loss |> Math.Abs
    let ev = win_pct * avg_win - (1m - win_pct) * (avg_loss |> Math.Abs)

    let gains = outcomes |> Seq.map (fun o -> o.PercentGain)
    let gainDistribution = core.fs.Services.Analysis.DistributionStatistics.calculate gains
    
    // return trade summary
    {
        StrategyName = strategyName
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

let parseTradeOutcomes (filepath:string) = TradeOutcomeOutput.Load(filepath).Rows

let verifyRecords (input:StudyInput) =
    let records = input.Rows
    // make sure there is at least some records in here, ideally in thousands
    let numberOfRecords = records |> Seq.length
    match numberOfRecords with
    | 0 -> failwith "no records"
    | x when x < Constants.MinimumRecords -> failwith $"not enough records: {x}"
    | _ -> ()
    
    // make sure all dates are as expected
    let datesFine = records |> Seq.forall (fun r -> r.Date.Year >= Constants.EarliestYear && r.Date.Year <= Constants.LatestYear)
    match datesFine with
    | false -> failwith "date is out of range"
    | true -> ()
    
    // make sure all tickers are set
    let tickersFine = records |> Seq.forall (fun r -> String.IsNullOrWhiteSpace(r.Ticker) = false)
    match tickersFine with
    | false -> failwith "ticker is blank"
    | true -> ()
    
    // make sure all screenerIds are set
    let screenerIdsFine = records |> Seq.forall (fun r -> r.Screenerid <> 0)
    match screenerIdsFine with
    | false -> failwith "screenerid is blank"
    | true -> ()
    
    records

type Unified =
    | Input of StudyInput.Row
    | Output of GapStudyOutput.Row
    
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
    printfn $"Minimum date: %A{minimumDate.Date}"
    printfn $"Maximum date: %A{maximumDate.Date}"
    printfn ""
    
let getEarliestDateByTicker (records:StudyInput.Row seq) =
    
    records
        |> Seq.groupBy (fun r -> r.Ticker)
        |> Seq.map (fun (ticker, records) ->
            let earliestDate = records |> Seq.minBy (fun r -> r.Date)
            (ticker, earliestDate)
        )
        
let study (inputFilename:string) (outputFilename:string) (priceFunc:DateTimeOffset -> DateTimeOffset -> Ticker -> Async<PriceBars option>) = async {
    // parse and verify
    let records =
        inputFilename
        |> StudyInput.Load
        |> verifyRecords
        
    // describe records
    records |> Seq.map (fun r -> Input r) |> describeRecords
        
    // generate a pair of ticker and the earliest data it is seen
    let tickerDatePairs = records |> getEarliestDateByTicker
    
    // output how many records are left
    printfn $"Unique tickers: %d{tickerDatePairs |> Seq.length}"
    
    // when ready, for each ticker, get historical prices from price provider
    // starting with 365 days before the earliest date through today
    
    let! results =
        tickerDatePairs
        |> Seq.map (fun (ticker, earliestDate) -> async {
            
            let earliestDateMinus365 = earliestDate.Date.AddDays(-365)
            let today = DateTimeOffset.UtcNow
            
            let! prices = ticker |> Ticker |> priceFunc earliestDateMinus365 today
            return (ticker, prices)
        })
        |> Async.Parallel
        
    let failed = results |> Array.filter (fun (_, prices) -> prices.IsNone)
    let prices =
        results
        |> Array.choose (fun (ticker, prices) ->
            match prices with
            | Some prices -> Some (ticker, prices)
            | None -> None
        )
        |> Map.ofArray
    
    printfn $"Failed: %d{failed.Length}"
    printfn $"Succeeded: %d{prices.Count}"
    
    let recordsWithPrices = records |> Seq.filter (fun r -> prices.ContainsKey(r.Ticker))
    printfn $"Records with prices: %d{recordsWithPrices |> Seq.length}"
    
    // now we are interested in gap ups
    let gapUpIndex =
        prices |> Map.keys |> Seq.collect (fun key ->
            let bars = prices[key]
            let gaps = detectGaps bars bars.Length
            let gapUps =
                gaps
                |> Array.filter (fun (g:Gap) -> g.Type = GapType.Up)
                |> Array.map (fun (g:Gap) ->
                    let gapKey = (key, g.Bar.DateStr)
                    (gapKey,g)
                )
            gapUps
        )
        |> Map.ofSeq
        
        
    printfn $"Gap up index: %d{gapUpIndex.Count}"
    
    // go through the records and only keep the ones that have a gap up
    let updatedRecords =
        recordsWithPrices
        |> Seq.map (fun r ->
            let hasGapUp = gapUpIndex.ContainsKey(r.Ticker, r.Date.ToString("yyyy-MM-dd"))
            (r, hasGapUp)
        )
    
    printfn $"Updated records: %d{updatedRecords |> Seq.length}"
    
    // output records with gap ups into CSV
    let rows =
        updatedRecords
        |> Seq.map (fun (r,hasGapUp) ->
            let row = GapStudyOutput.Row(ticker=r.Ticker, date=r.Date, screenerid=r.Screenerid, hasGapUp=hasGapUp)
            row
        )
       
    let csvOutput = new GapStudyOutput(rows)
    csvOutput.Save outputFilename
}
    
let runStrategy strategy dataWithPriceBars =
    dataWithPriceBars
    |> Seq.map(fun (r, prices) ->
        let tradeOutcome = strategy prices r
        tradeOutcome
    )
    
let runTrades (matchedInputFilename:string) (priceFunc:string -> Async<PriceBars>) = async {
    
    let data =
        matchedInputFilename
        |> GapStudyOutput.Load
        |> fun x -> x.Rows
    
    data |> Seq.map Output |> describeRecords
    
    // ridiculous, sometimes data provider does not have prices for the date
    // so we filter those records out
    let! asyncData =
        data
        |> Seq.map (fun r -> async {
            let! prices = r.Ticker |> priceFunc
            let startBar = r.Date |> prices.TryFindByDate
            return (r, prices, startBar)   
        })
        |> Async.Parallel
        
    let dataWithPriceBars =
        asyncData
        |> Seq.choose (fun (r,prices,startBar) ->
            match startBar with
            | None -> None
            | Some _ -> Some (r, prices)
        )
    
    printfn "Ensured that data has prices"
    
    dataWithPriceBars |> Seq.map fst |> Seq.map Output |> describeRecords
       
    printfn "Executing trades..."
    
    let strategies = [
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices (Some 5))
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices (Some 10))
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices (Some 30))
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices (Some 60))
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices (Some 90))
        (fun prices -> TradingStrategies.buyAndHoldStrategy prices None)
    ]
    
    let allOutcomes =
        strategies
        |> Seq.map (fun strategy ->
            let outcomes = runStrategy strategy dataWithPriceBars  
            outcomes
        )
        |> Seq.concat
        
    return allOutcomes
}
    
let saveOutcomes (outputPath:string) outcomesByStrategy =
    
    let csvOutput = new TradeOutcomeOutput(outcomesByStrategy)
    csvOutput.Save outputPath