namespace core.fs.Services.Trading

open System
open System.Collections.Generic
open core.Account
open core.Shared
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Stocks
open core.fs.Services.Trading

[<AbstractClass>]
type TradingStrategy(name:string) =
    
    let mutable _numberOfSharesAtStart = 0m
    
    static member ClosePosition price date (position:StockPositionState) =
        match position.IsOpen with
        | true -> position |> StockPosition.close price date
        | false -> position
    
    static member CalculateMaxDrawdownAndGain (last10Bars:seq<PriceBar>) =
        
        let maxDrawdownPctRecent,maxGainPctRecent =
            last10Bars
            |> Seq.fold (fun (maxDrawdownPctRecent,maxGainPctRecent) bar ->
                (
                    let referenceBar = last10Bars |> Seq.head
                    Math.Min(maxDrawdownPctRecent,bar.PercentDifferenceFromLow(referenceBar.Close)),
                    Math.Max(maxGainPctRecent,bar.PercentDifferenceFromHigh(referenceBar.Close))
                )
            ) (0m,0m)
        
        (maxDrawdownPctRecent,maxGainPctRecent)
    
    member this.Name = name
    member this.NumberOfSharesAtStart = _numberOfSharesAtStart
    
    abstract member ApplyPriceBarToPositionInternal : SimulationContext -> PriceBar -> StockPositionState
    
    member this.ApplyPriceBarToPosition (context:SimulationContext) (bar:PriceBar) =
        
        match context.Position.Closed with
        | Some _ -> context
        | None -> 
            let appliedPosition = this.ApplyPriceBarToPositionInternal context bar
            
            let last10bars = context.Last10Bars
            
            if last10bars.Count = 10 then
                last10bars.RemoveAt(0)
                
            last10bars.Add(bar)
            
            let calculations = appliedPosition |> StockPositionWithCalculations
            
            {
                Position = appliedPosition
                MaxDrawdown = Math.Min(context.MaxDrawdown,bar.PercentDifferenceFromLow(calculations.AverageBuyCostPerShare))
                MaxGain = Math.Max(context.MaxGain,bar.PercentDifferenceFromHigh(calculations.AverageBuyCostPerShare))
                Last10Bars = last10bars
            }
           
    interface  ITradingStrategy with
    
        member this.Run (bars:PriceBars) closeIfOpen (position:StockPositionState) =
            
            let context = 
                {
                    Position = position
                    MaxDrawdown = 0m
                    MaxGain = 0m
                    Last10Bars = List<PriceBar>(10)
                }
                
            _numberOfSharesAtStart <- position.NumberOfShares |> abs
                
            let finalContext =
                bars.Bars
                |> Seq.fold this.ApplyPriceBarToPosition context
                
            let maxDrawdownPctRecent,maxGainPctRecent = TradingStrategy.CalculateMaxDrawdownAndGain finalContext.Last10Bars
            
            let positionWithCalculations =
                match closeIfOpen && finalContext.Position.IsClosed = false with
                | true ->
                    let closingPrice = finalContext.Last10Bars[finalContext.Last10Bars.Count - 1].Close
                    let closingDate = finalContext.Last10Bars[finalContext.Last10Bars.Count - 1].Date
                    
                    finalContext.Position |> TradingStrategy.ClosePosition closingPrice closingDate
                | false -> finalContext.Position
                |> StockPositionWithCalculations
            
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxGainPct = finalContext.MaxGain
                MaxDrawdownPctRecent = maxDrawdownPctRecent
                MaxGainPctRecent = maxGainPctRecent
                Position = positionWithCalculations
                StrategyName = this.Name
            }

type TradingStrategyCloseOnCondition(name:string,exitCondition) =
    
    inherit TradingStrategy(name)
    
    override this.ApplyPriceBarToPositionInternal (context:SimulationContext) (bar:PriceBar) =
        if exitCondition context bar then
            context.Position
            |> TradingStrategy.ClosePosition bar.Close bar.Date
        else
            context.Position
            

type TradingStrategyActualTrade() =
    
    interface ITradingStrategy with
    
        member this.Run (bars:PriceBars) (closeIfOpen:bool) (position:StockPositionState) =
            
            let finalPosition, maxDrawdownPct, maxGainPct, last10Bars =
                bars.Bars
                |> Seq.fold (fun (position:StockPositionState, maxDrawdownPct, maxGainPct, last10Bars:PriceBar list) bar ->
                    if position.IsClosed && bar.Date.Date = position.Closed.Value.Date then
                        position, maxDrawdownPct, maxGainPct, last10Bars
                    else
                        let calculation = position |> StockPositionWithCalculations
                        let maxDrawdownPct = Math.Min(maxDrawdownPct,bar.PercentDifferenceFromLow(calculation.AverageBuyCostPerShare))
                        let maxGainPct = Math.Max(maxGainPct,bar.PercentDifferenceFromHigh(calculation.AverageBuyCostPerShare))
                        
                        let newLast10Bars =
                            match last10Bars.Length with
                            | x when x = 10 -> last10Bars[1..9] @ [bar]
                            | _ -> last10Bars @ [bar]
                            
                        position, maxDrawdownPct, maxGainPct, newLast10Bars  
                ) (position, Decimal.MaxValue, Decimal.MinValue, [])
                
            let maxDrawdownPctRecent,maxGainPctRecent = TradingStrategy.CalculateMaxDrawdownAndGain(last10Bars)
            
            let positionWithCalculations =
                match closeIfOpen with
                | true ->
                    let closingPrice = last10Bars[last10Bars.Length - 1].Close
                    let closingDate = last10Bars[last10Bars.Length - 1].Date
                    
                    finalPosition |> TradingStrategy.ClosePosition closingPrice closingDate
                | false -> finalPosition
                |> StockPositionWithCalculations
                
            {
                MaxDrawdownPct = maxDrawdownPct
                MaxGainPct = maxGainPct
                MaxDrawdownPctRecent = maxDrawdownPctRecent
                MaxGainPctRecent = maxGainPctRecent
                Position = positionWithCalculations
                StrategyName = TradingStrategyConstants.ActualTradesName
            }    

type TradingStrategyWithProfitPoints(name:string,numberOfProfitPoints,profitPointFunc,stopPriceFunc) =
    
    inherit TradingStrategy(name)
    
    let mutable _level = 1
    
    member this.ExecuteProfitTake sellPrice date (position:StockPositionState) =
        
        let executeProfitTake portion price date (position:StockPositionState) =
            match position.IsShort with
            | true -> position |> StockPosition.buy portion price date
            | false -> position |> StockPosition.sell portion price date
            
        // figure out how much to sell based on the number of profit points
        // and how many shares we have left
        let portion =
            if _level = numberOfProfitPoints then
                position.NumberOfShares
            else
                match int (this.NumberOfSharesAtStart / decimal numberOfProfitPoints) |> decimal with
                | 0m -> 1m
                | x when x > (abs position.NumberOfShares) -> position.NumberOfShares    
                | x -> x
            |> abs // it can be negative if we're shorting
        
        let adjustStopIfNecessary (position:StockPositionState) =
            match position.IsOpen with
            | true ->
                let stopPrice:decimal = position |> StockPositionWithCalculations |> stopPriceFunc _level
                position |> StockPosition.setStop (Some stopPrice) date
            | _ -> position
        
            
        let afterSell =
            position
            |> executeProfitTake portion sellPrice date
            |> adjustStopIfNecessary
        
        _level <- _level + 1
        
        afterSell
            
    override this.ApplyPriceBarToPositionInternal context bar =
        
        let profitPrice = context.Position |> StockPositionWithCalculations |> profitPointFunc _level
                
        let stopReached price (position:StockPositionState) =
            match position.StopPrice with
            | Some stopPrice ->
                match position.IsShort with
                | true -> price >= stopPrice
                | false -> price <= stopPrice
            | _ -> false
            
        let profitTakeReached (bar:PriceBar) (position:StockPositionState) =
            match position.IsShort with
            | true -> bar.Low < profitPrice
            | false -> bar.High > profitPrice
        
        let executeProfitTakeIfNecessary (position:StockPositionState) =
            match profitTakeReached bar position with
            | true -> this.ExecuteProfitTake profitPrice bar.Date position
            | false -> position
        
        let closeIfNecessary (position:StockPositionState) =
            match stopReached bar.Close position with
            | true -> TradingStrategy.ClosePosition bar.Close bar.Date position
            | _ -> position
        
        context.Position
        |> executeProfitTakeIfNecessary
        |> closeIfNecessary
            
            
module TradingStrategyFactory =
    
    let advancingStop (level:int) (position:StockPositionWithCalculations) rrFunc =
        match level with
        | 1 -> position.AverageCostPerShare
        | _ -> rrFunc (level - 1) position
    
    let delayedAdvancingStop (level:int) (position:StockPositionWithCalculations) (rrLevelFunc:int -> decimal) =
        match level with
        | 1 -> position.StopPrice.Value
        | 2 -> position.AverageCostPerShare
        | _ -> rrLevelFunc (level - 2)
    
    let createActualTrade() : ITradingStrategy = TradingStrategyActualTrade()
    let createProfitPointsTrade numberOfProfitPoints : ITradingStrategy =
        let stopFunc = fun (level:int) position -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
        TradingStrategyWithProfitPoints($"Profit points: {numberOfProfitPoints}", numberOfProfitPoints, ProfitPoints.getProfitPointWithStopPrice, stopFunc)
    
    let createProfitPointsBasedOnPctGainTrade percentGain numberOfProfitPoints : ITradingStrategy =
        let profitPointFunc = fun (level:int) -> ProfitPoints.getProfitPointWithPercentGain level percentGain
        let stopFunc = fun (level:int) (position:StockPositionWithCalculations) -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
        TradingStrategyWithProfitPoints($"{numberOfProfitPoints} profit points at {percentGain}%% intervals", numberOfProfitPoints, profitPointFunc, stopFunc)
        
    let createCloseAfterFixedNumberOfDays numberOfDays : ITradingStrategy =
        let exitCondition = fun (context:SimulationContext) (bar:PriceBar) -> bar.Date - context.Position.Opened > TimeSpan.FromDays(float numberOfDays)
        TradingStrategyCloseOnCondition($"Close after {numberOfDays} days", exitCondition)
    
    let getStrategies() : ITradingStrategy seq =
        [
            createProfitPointsTrade 3 // 3 profit points
            createProfitPointsBasedOnPctGainTrade TradingStrategyConstants.AvgPercentGain 3
            createCloseAfterFixedNumberOfDays 15
            createCloseAfterFixedNumberOfDays 30
        ]
    
type TradingStrategyRunner(brokerage:IBrokerageGetPriceHistory, hours:IMarketHours) =
    
    let setRiskAmountFromActualTradeIfSet actualTrade date stockPosition =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            match actualTrade.RiskAmount with
            | Some riskAmount -> stockPosition |> StockPosition.setRiskAmount riskAmount date
            | None -> stockPosition
    
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice,
            ticker:Ticker,
            ``when``:DateTimeOffset,
            closeIfOpenAtTheEnd:bool,
            actualTrade:StockPositionState option) =
        
        task {
            // when we simulate a purchase for that day, assume it's end of the day
            // so that the price feed will return data from that day and not the previous one
            let convertedWhen = hours.GetMarketEndOfDayTimeInUtc(``when``)
            
            let! prices =
                brokerage.GetPriceHistory
                    user
                    ticker
                    PriceFrequency.Daily
                    convertedWhen
                    (TradingStrategyConstants.MaxNumberOfDaysToSimulate |> convertedWhen.AddDays)
                    
            let results = TradingStrategyResults()
            
            match prices.Success with
            | None -> results.MarkAsFailed($"Failed to get price history for {ticker}: {prices.Error.Value.Message}")
            | Some bars ->
                match bars.Bars with
                | [||] -> results.MarkAsFailed($"No price history found for {ticker}")
                | _ ->
                    
                    TradingStrategyFactory.getStrategies()
                    |> Seq.iter ( fun strategy ->
                        
                        let result =
                            StockPosition.``open`` ticker numberOfShares price ``when``
                            |> StockPosition.setStop stopPrice ``when``
                            |> setRiskAmountFromActualTradeIfSet actualTrade ``when``
                            |> strategy.Run bars closeIfOpenAtTheEnd
                            
                        results.Add(result)
                    )
                    
                    match actualTrade with
                    | Some actualTrade ->
                        let actualResult = TradingStrategyFactory.createActualTrade().Run bars closeIfOpenAtTheEnd actualTrade
                        results.Insert(0, actualResult)
                    | None -> ()
            
            
            return results
        }
        
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice,
            ticker:Ticker,
            ``when``:DateTimeOffset,
            closeIfOpenAtTheEnd:bool) =
        
        this.Run(
            user,
            numberOfShares,
            price,
            stopPrice,
            ticker,
            ``when``,
            closeIfOpenAtTheEnd,
            actualTrade = None)
        
    member this.Run(user:UserState, position:StockPositionState,closeIfOpenAtTheEnd) =
        
        let calculations = position |> StockPositionWithCalculations
        
        let stopPrice =
            match calculations.FirstStop with
            | None -> Some (calculations.CompletedPositionCostPerShare * TradingStrategyConstants.DefaultStopPriceMultiplier)
            | _ -> calculations.FirstStop
            
        let numberOfShares =
            match position.IsShort with
            | true -> calculations.CompletedPositionShares * -1m
            | false -> calculations.CompletedPositionShares
                
        this.Run(
            user=user,
            numberOfShares=numberOfShares,
            price=calculations.CompletedPositionCostPerShare,
            stopPrice = stopPrice,
            ticker = position.Ticker,
            ``when``=position.Opened,
            closeIfOpenAtTheEnd=closeIfOpenAtTheEnd,
            actualTrade = Some position
        )
    