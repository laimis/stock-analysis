module studies.Trading

    open System
    open System.Collections.Generic
    open core.fs.Adapters.Stocks
    open core.fs.Services.Analysis
    open studies.DataHelpers
    open FSharp.Data
    
    type TradeOutcomeOutput =
        CsvProvider<
            Sample = "screenerid (int option), date (string), ticker (string), gap (decimal option), sma20 (decimal option), sma50 (decimal option), sma150 (decimal option), sma200 (decimal option), strategy (string), longOrShort (string), opened (string), openPrice (decimal), closed (string), closePrice (decimal), percentGain (decimal), numberOfDaysHeld (int)",
            HasHeaders=true
        >

    module TradeOutcomeOutput =

        let create strategy positionType (signal:ISignal) (openBar:PriceBar) (closeBar:PriceBar) =

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
            let gainDistribution = DistributionStatistics.calculate gains

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
    
    let private validateStopLossPercent stopLossPercent =
        match stopLossPercent with
        | Some stopLossPercent when stopLossPercent > 1m -> failwith $"Stop loss percent {stopLossPercent} is greater than 1"
        | Some stopLossPercent when stopLossPercent < 0m -> failwith $"Stop loss percent {stopLossPercent} is less than 0"
        | _ -> ()
            
    let private findNextDayBarAndIndex (signal:ISignal) (prices:PriceBars) =
        match prices.TryFindByDate signal.Date with
        | None -> failwith $"Could not find the price bar for the {signal.Ticker} @ {signal.Date}, unexpected"
        | Some openBarWithIndex ->
            let index,_ = openBarWithIndex
            let nextDayIndex = index + 1
            match nextDayIndex >= prices.Length with
            | true -> failwith $"No open day available for {signal.Ticker} @ {signal.Date}"
            | false -> (prices.Bars[nextDayIndex], nextDayIndex)

    let strategyWithGenericStopLoss verbose name positionType stopLossReachedFunc (signal:ISignal,prices:PriceBars) =
        let openBar, openDayIndex = findNextDayBarAndIndex signal prices
        
        if verbose then printfn $"Open bar for %s{signal.Ticker} on %A{signal.Date} is %A{openBar.Date} @ %A{openBar.Open}"
                
        // find the close day
        let closeBar =
            prices.Bars
            |> Seq.indexed
            |> Seq.skip openDayIndex
            |> Seq.map stopLossReachedFunc
            |> Seq.filter (fun (_, _, stopLossReached, closeDayReached) -> stopLossReached || closeDayReached)
            |> Seq.tryHead
            
        let closeBar =
            match closeBar with
            | None ->
                //if verbose then printfn $"Could not find close bar for %s{signal.Ticker} on %A{signal.Date}"
                failwith $"Could not find close bar for %s{signal.Ticker} on %A{signal.Date}"
            | Some (_, closeBar:PriceBar, stopLossReached, _) ->
                let reason = if stopLossReached then "stop loss reached" else "close day reached"
                if verbose then printfn $"Close bar for %s{signal.Ticker} on %A{signal.Date} is %A{closeBar.Date} because {reason}"
                closeBar
        
        TradeOutcomeOutput.create name positionType signal openBar closeBar

    let strategyWithStopLossPercent verbose positionType numberOfBarsToHold (stopLossPercent:decimal option) (signal:ISignal,prices:PriceBars) =
        
        validateStopLossPercent stopLossPercent
        
        let stopLossPortion =
            match stopLossPercent with
            | None -> ""
            | Some stopLossPercent -> "SL of " + stopLossPercent.ToString("0.00") + "%"
            
        let holdPeriod =
            match numberOfBarsToHold with
            | None -> ""
            | Some numberOfBarsToHold -> "hold for " + numberOfBarsToHold.ToString() + " bars"
            
        let buyOrSell =
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> "Buy"
            | core.fs.Stocks.StockPositionType.Short -> "Sell"
            
        let name = String.concat " " ([buyOrSell; holdPeriod; stopLossPortion] |> List.filter (fun x -> x <> ""))
            
        let openDay, openDayIndex = findNextDayBarAndIndex signal prices
        
        let closeBar =
            match numberOfBarsToHold with
            | None -> prices.Last
            | Some numberOfBarsToHold ->
                let closeDayIndex = openDayIndex + numberOfBarsToHold
                match closeDayIndex with
                | x when x >= prices.Length -> prices.Last
                | _ -> prices.Bars[closeDayIndex]
                
        let stopPrice =
            match stopLossPercent with
            | None ->
                match positionType with
                | core.fs.Stocks.StockPositionType.Long -> 0m
                | core.fs.Stocks.StockPositionType.Short -> Decimal.MaxValue
            | Some stopLossPercent ->
                let multiplier =
                    match positionType with
                    | core.fs.Stocks.StockPositionType.Long -> 1m - stopLossPercent
                    | core.fs.Stocks.StockPositionType.Short -> 1m + stopLossPercent
                openDay.Open * multiplier
        
        let stopLossFunc (index, bar:PriceBar) =
            let stopPriceReached =
                match positionType with
                | core.fs.Stocks.StockPositionType.Long -> bar.Close < stopPrice
                | core.fs.Stocks.StockPositionType.Short -> bar.Close > stopPrice
                
            let closeDayReached = bar.Date >= closeBar.Date
            (index, bar, stopPriceReached, closeDayReached)
        
        strategyWithGenericStopLoss verbose name positionType stopLossFunc (signal,prices)
        
    let strategyWithSignalOpenAsStop verbose (signal:ISignal,prices:PriceBars) =
        
        let name = "Buy and use signal open as stop"
            
        let closeBar = prices.Last        
        let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> snd |> _.Open
        
        let stopLossFunc (index, bar:PriceBar) =
            let stopPriceReached = bar.Close < stopPrice
            let closeDayReached = bar.Date >= closeBar.Date
            (index, bar, stopPriceReached, closeDayReached)
        
        strategyWithGenericStopLoss verbose name core.fs.Stocks.Long stopLossFunc (signal,prices)
        
    let strategyWithSignalCloseAsStop verbose (signal:ISignal,prices:PriceBars) =
        
        let name = "Buy and use signal close as stop"
            
        let closeBar = prices.Last        
        let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> snd |> _.Close
        
        let stopLossFunc (index, bar:PriceBar) =
            let stopPriceReached = bar.Close < stopPrice
            let closeDayReached = bar.Date >= closeBar.Date
            (index, bar, stopPriceReached, closeDayReached)
        
        strategyWithGenericStopLoss verbose name core.fs.Stocks.Long stopLossFunc (signal,prices)
        
        
    let strategyWithTrailingStop verbose positionType stopLossPercent (signal:ISignal,prices:PriceBars) =
        
        stopLossPercent |> Some |> validateStopLossPercent
        
        let buyOrSell = 
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> "Buy"
            | core.fs.Stocks.StockPositionType.Short -> "Sell"
            
        let name = $"{buyOrSell} and use trailing stop, SL of {stopLossPercent}%%"
        
        let stopLossReferencePrice = ref 0m
        
        let closeBar = prices.Last
        
        let stopLossFunc (index, bar:PriceBar) =
            let closeDayReached = bar.Date >= closeBar.Date
            
            if stopLossReferencePrice.Value = 0m then
                stopLossReferencePrice.Value <- bar.Close
            
            let refValue, stopReached =
                match positionType with
                | core.fs.Stocks.StockPositionType.Long ->
                    Math.Max(bar.Close, stopLossReferencePrice.Value), bar.Close < stopLossReferencePrice.Value * (1m - stopLossPercent)
                | core.fs.Stocks.StockPositionType.Short ->
                    Math.Min(bar.Close, stopLossReferencePrice.Value), bar.Close > stopLossReferencePrice.Value * (1m + stopLossPercent)
                    
            stopLossReferencePrice.Value <- refValue
            
            (index, bar, stopReached, closeDayReached)
        
        strategyWithGenericStopLoss verbose name positionType stopLossFunc (signal,prices)

    let private prepareSignalsForTradeSimulations (priceFunc:string -> Async<PriceBars>) signals = async {
        
        signals |> describeSignals
        
        // ridiculous, sometimes data provider does not have prices for the date,
        // so we filter those records out
        let! asyncData =
            signals
            |> Seq.map (fun r -> async {
                let! prices = r.Ticker |> priceFunc
                let startBar = r.Date |> prices.TryFindByDate
                return (r, prices, startBar)   
            })
            |> Async.Sequential
            
        let signalsWithPriceBars =
            asyncData
            |> Seq.choose (fun (r,prices,startBar) ->
                match startBar with
                | None -> None
                | Some _ -> Some (r, prices)
            )
            |> Seq.sortBy (fun (r,_) -> r.Date)
            |> Seq.toList
        
        printfn "Ensured that data has prices"
        
        signalsWithPriceBars |> Seq.map fst |> describeSignals
        
        return signalsWithPriceBars
    }
        
    let runTrades getPricesFunc signals strategies = async {
        
        let! signalsWithPriceBars =
            signals
            |> prepareSignalsForTradeSimulations getPricesFunc
        
        printfn "Executing trades..."
        
        let allOutcomes =
            strategies
            |> List.map (fun strategy ->
                
                // track outcomes by ticker and open/close dates
                let map = Dictionary<string, List<TradeOutcomeOutput.Row>>()
                
                signalsWithPriceBars
                |> List.map strategy
                |> List.map (fun (outcome:TradeOutcomeOutput.Row) ->
                    match map.TryGetValue(outcome.Ticker) with
                    | false, _ ->
                        let list = List<TradeOutcomeOutput.Row>()
                        list.Add(outcome)
                        map.Add(outcome.Ticker, list)
                        Some outcome
                    | true, list ->
                        let fallsInsideExistingTrade = list |> Seq.exists (fun (x:TradeOutcomeOutput.Row) -> outcome.Opened >= x.Opened && outcome.Opened <= x.Closed)
                        match fallsInsideExistingTrade with
                        | true -> None
                        | false ->
                            list.Add(outcome)
                            Some outcome
                )
                |> List.choose id
            )
            |> List.concat
            
        return allOutcomes
    }
