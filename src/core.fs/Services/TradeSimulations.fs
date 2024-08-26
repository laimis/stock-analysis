namespace core.fs.Services.Trading

open System
open core.Account
open core.Shared
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Stocks
open core.fs.Services.Trading

[<AbstractClass>]
type TradingStrategy(name:string) =
    
    let mutable _numberOfSharesAtStart = 0m
    
    static member GetPercentDiffFromReferencePrice (bar:PriceBar) (context:SimulationContext) =
        let referencePrice = context.Position.PositionOpenPrice
        match context.Position.IsShort with
        | true -> (referencePrice - bar.Close) / referencePrice
        | false -> bar.percentDifferenceFromClose(referencePrice)
        
    static member CalculateMAEPercentage (context:SimulationContext) (bar:PriceBar) =
        let difference = context |> TradingStrategy.GetPercentDiffFromReferencePrice bar
        Math.Min(context.MaxDrawdown, difference)    
    
    static member CalculateMFEPercentage (context:SimulationContext) (bar:PriceBar) =
        let difference = context |> TradingStrategy.GetPercentDiffFromReferencePrice bar
        Math.Max(context.MaxGain, difference)
    
    static member ClosePosition price date (position:StockPositionState) =
        match position.IsOpen with
        | true -> position |> StockPosition.close price date
        | false -> position
        
    static member ForceCloseIfNecessary closeIfOpen (context:SimulationContext) =
        match closeIfOpen && context.Position.IsOpen with
        | true ->
            (context.Position.GetPositionState()
            |> TradingStrategy.ClosePosition context.LastBar.Close context.LastBar.Date
            |> StockPositionWithCalculations, true)
        | false -> (context.Position, false)
    
    member this.Name = name
    member this.NumberOfSharesAtStart = _numberOfSharesAtStart
    
    abstract member ApplyPriceBarToPositionInternal : SimulationContext -> PriceBar -> StockPositionState
    
    member private this.ApplyPriceBarToPosition (context:SimulationContext) (bar:PriceBar) =
        
        match context.Position.Closed with
        | Some _ -> context
        | None ->
            let latestPositionState = this.ApplyPriceBarToPositionInternal context bar
            
            {
                Position = latestPositionState |> StockPositionWithCalculations
                MaxDrawdown = TradingStrategy.CalculateMAEPercentage context bar
                MaxGain = TradingStrategy.CalculateMFEPercentage context bar
                LastBar = bar 
            }
           
    interface  ITradingStrategy with
    
        member this.Run (bars:PriceBars) closeIfOpen (position:StockPositionState) =
            
            let context = 
                {
                    Position = position |> StockPositionWithCalculations
                    MaxDrawdown = Decimal.MaxValue
                    MaxGain = Decimal.MinValue
                    LastBar = bars.Bars[0] 
                }
                
            _numberOfSharesAtStart <- position.NumberOfShares |> abs
                
            let finalContext =
                bars.Bars
                |> Seq.fold this.ApplyPriceBarToPosition context
                
            let finalPosition, forcedClosed = TradingStrategy.ForceCloseIfNecessary closeIfOpen finalContext
            
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxGainPct = finalContext.MaxGain
                Position = finalPosition
                StrategyName = this.Name
                ForcedClosed = forcedClosed 
            }

type TradingStrategyCloseOnCondition(name:string,exitCondition) =
    
    inherit TradingStrategy(name)
    
    override this.ApplyPriceBarToPositionInternal (context:SimulationContext) (bar:PriceBar) =
        let doExit, stopPrice = exitCondition context bar
        
        let positionAfterStopAdjustment =
            match stopPrice with
            | Some sp ->
                context.Position.GetPositionState() |> StockPosition.setStop stopPrice bar.Date
            | None -> context.Position.GetPositionState()
            
        if doExit then
            positionAfterStopAdjustment |> TradingStrategy.ClosePosition bar.Close bar.Date
        else
            positionAfterStopAdjustment

type TradingStrategyActualTrade() =
    
    interface ITradingStrategy with
    
        member this.Run (bars:PriceBars) (closeIfOpen:bool) (position:StockPositionState) =
            
            let calcs = position |> StockPositionWithCalculations
            let finalContext =
                bars.Bars
                |> Seq.fold (fun (context:SimulationContext) bar ->
                    if position.IsClosed && bar.Date.Date > position.Closed.Value.Date then
                        context
                    else
                        let maxDrawdownPct = TradingStrategy.CalculateMAEPercentage context bar
                        let maxGainPct = TradingStrategy.CalculateMFEPercentage context bar
                        
                        { Position = calcs; MaxDrawdown = maxDrawdownPct; MaxGain = maxGainPct; LastBar = bar }
                ) {Position = calcs; MaxDrawdown = Decimal.MaxValue; MaxGain = Decimal.MinValue; LastBar = bars.Bars[0]}
                
            let finalPosition, forcedClosed = TradingStrategy.ForceCloseIfNecessary closeIfOpen finalContext
                
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxGainPct = finalContext.MaxGain
                Position = finalPosition
                StrategyName = TradingStrategyConstants.ActualTradesName
                ForcedClosed = forcedClosed
            }

type TradingStrategyWithProfitPoints(name:string,numberOfProfitPoints,profitPointFunc,stopPriceFunc) =
    
    inherit TradingStrategy(name)
    
    let mutable _level = 1
    
    member this.ExecuteProfitTake sellPrice date (position:StockPositionWithCalculations) =
        
        let executeProfitTake portion price date (position:StockPositionWithCalculations) =
            match position.IsShort with
            | true -> position.GetPositionState() |> StockPosition.buy portion price date
            | false -> position.GetPositionState() |> StockPosition.sell portion price date
            
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
        
        let profitPrice = context.Position |> profitPointFunc _level
                
        let stopReached price (position:StockPositionState) =
            match position.StopPrice with
            | Some stopPrice ->
                match position.StockPositionType with
                | Short -> price >= stopPrice
                | Long -> price <= stopPrice
            | _ -> false
            
        let profitTakeReached (bar:PriceBar) (position:StockPositionWithCalculations) =
            match position.IsShort with
            | true -> bar.Low < profitPrice
            | false -> bar.High > profitPrice
        
        let executeProfitTakeIfNecessary (position:StockPositionWithCalculations) =
            match profitTakeReached bar position with
            | true -> this.ExecuteProfitTake profitPrice bar.Date position
            | false -> position.GetPositionState()
        
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
        let exitCondition = fun (context:SimulationContext) (bar:PriceBar) ->
            let doExit = bar.Date - context.Position.Opened > TimeSpan.FromDays(float numberOfDays)
            doExit, None
        TradingStrategyCloseOnCondition($"Close after {numberOfDays} days", exitCondition)
        
    let createCloseAfterFixedNumberOfDaysWithStop (stopDescription:string) (numberOfDays:int) (stopPrice:decimal) : ITradingStrategy =
        let daysHeldReached = fun (context:SimulationContext) (bar:PriceBar) -> bar.Date - context.Position.Opened > TimeSpan.FromDays(float numberOfDays)
        let stopReached = fun (context:SimulationContext) (bar:PriceBar) ->
            match context.Position.IsShort with
            | true -> bar.Close > stopPrice
            | false -> bar.Close < stopPrice
        let exitCondition = fun (context:SimulationContext) (bar:PriceBar) ->
            let doExit = daysHeldReached context bar || stopReached context bar
            (doExit, Some stopPrice)
        TradingStrategyCloseOnCondition($"Close after {numberOfDays} days with {stopDescription} stop", exitCondition)
        
    let createTrailingStop (stopDescription:string) (trailingPercentage:decimal) (initialStop:decimal option)  : ITradingStrategy =
        if trailingPercentage <= 0m then
            failwith "Trailing percentage must be greater than 0"
        if trailingPercentage >= 1m then
            failwith "Trailing percentage must be less than 1"
        if initialStop.IsSome && initialStop.Value < 0m then
            failwith "Initial stop must be greater or equal to 0"
        
        let latestStop =
            match initialStop with
            | Some stop -> ref stop
            | None -> ref 0m
            
        let stopReached = fun (context:SimulationContext) (bar:PriceBar) ->
            let stopReached =
                match context.Position.IsShort with
                | true -> bar.Close > latestStop.Value && latestStop.Value <> 0m
                | false -> bar.Close < latestStop.Value
            
            match stopReached with
            | true ->
                true, Some latestStop.Value
            | false ->
                
                match context.Position.IsShort with
                | true ->
                    let newStopPriceCandidate = bar.Close * (1m + trailingPercentage)
                    // the initial stop is 0m if one is not set explicitly
                    // and that breaks the math.min logic as that initial stop will always be selected
                    // so we need to handle that appropriately
                    latestStop.Value <-
                        match latestStop.Value with
                        | 0m -> newStopPriceCandidate
                        | _ -> Math.Min(latestStop.Value,newStopPriceCandidate)
                | false ->
                    let newStopPriceCandidate = bar.Close * (1m - trailingPercentage)
                    latestStop.Value <- Math.Max(latestStop.Value,newStopPriceCandidate)
                
                (false, Some latestStop.Value)
                
        TradingStrategyCloseOnCondition($"Trailing stop with {stopDescription}", stopReached)
        
    let createLastSellStrategy (position:StockPositionState) : ITradingStrategy =
        let exitCondition = fun (_:SimulationContext) (bar:PriceBar) ->
            let doExit = position.Closed.IsSome && bar.Date.Date = position.Closed.Value.Date
            doExit, None
        TradingStrategyCloseOnCondition("Close last sell", exitCondition)
    
    let getStrategies (actualTrade:StockPositionState option) : ITradingStrategy seq =
        let firstStop (position:StockPositionState) =
            let defaultStop =
                match position.StockPositionType with
                | Short -> Decimal.MaxValue
                | Long -> 0m
            position |> StockPositionWithCalculations |> _.FirstStop() |> Option.defaultValue defaultStop
            
        let percentStopBasedOnCostPerShare (percentage:decimal) (position:StockPositionState) =
            let costPerShare = position |> StockPositionWithCalculations |> _.CompletedPositionCostPerShare
            let multiplier =
                match position.StockPositionType with
                | Short -> 1m + percentage
                | Long -> 1m - percentage
            costPerShare * multiplier
            
        [
            createProfitPointsTrade 3
            createProfitPointsBasedOnPctGainTrade TradingStrategyConstants.AvgPercentGain 3
            createCloseAfterFixedNumberOfDays 15
            createCloseAfterFixedNumberOfDays 30
            createTrailingStop "5%" 0.05m None
            createTrailingStop "10%" 0.10m None
            createTrailingStop "20%" 0.20m None
            if actualTrade.IsSome then
                createLastSellStrategy actualTrade.Value
                createCloseAfterFixedNumberOfDaysWithStop "size based" 30 (actualTrade.Value |> firstStop)
                createCloseAfterFixedNumberOfDaysWithStop "5% stop" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.05m)
                createCloseAfterFixedNumberOfDaysWithStop "10% stop" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.10m)
                createCloseAfterFixedNumberOfDaysWithStop "20% stop" 30 (actualTrade.Value |> percentStopBasedOnCostPerShare 0.20m)
                createTrailingStop "5% w/ initial stop" 0.05m (actualTrade.Value |> firstStop |> Some)
                createTrailingStop "10% w/ initial stop" 0.10m (actualTrade.Value |> firstStop |> Some)
                createTrailingStop "20% w/ initial stop" 0.20m (actualTrade.Value |> firstStop |> Some)
        ]
    
type TradingStrategyRunner(brokerage:IBrokerageGetPriceHistory, hours:IMarketHours) =
    
    let setRiskAmountFromActualTradeIfSet actualTrade date stockPosition =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            match actualTrade.RiskAmount with
            | Some riskAmount -> stockPosition |> StockPosition.setRiskAmount riskAmount date
            | None -> stockPosition
            
    let setLabelsFromActualTradeIfSet (actualTrade:StockPositionState option) date (stockPosition:StockPositionState) =
        match actualTrade with
        | None -> stockPosition
        | Some actualTrade ->
            actualTrade.Labels |> Seq.fold (fun position label -> position |> StockPosition.setLabel label.Key label.Value date) stockPosition
    
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
                    (Some convertedWhen)
                    (TradingStrategyConstants.MaxNumberOfDaysToSimulate |> convertedWhen.AddDays |> Some)
                    
            let results = TradingStrategyResults()
            
            match prices with
            | Error err -> results.MarkAsFailed($"Failed to get price history for {ticker}: {err.Message}")
            | Ok bars ->
                let stockPosition =
                    StockPosition.``open`` ticker numberOfShares price ``when``
                    |> StockPosition.setStop stopPrice ``when``
                    |> setRiskAmountFromActualTradeIfSet actualTrade ``when``
                    |> setLabelsFromActualTradeIfSet actualTrade ``when``
                    
                match bars.Bars with
                | [||] -> results.MarkAsFailed($"No price history found for {ticker}")
                | _ ->
                    
                    TradingStrategyFactory.getStrategies actualTrade
                    |> Seq.iter ( fun strategy ->
                        stockPosition |> strategy.Run bars closeIfOpenAtTheEnd |> results.Add
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
            match calculations.FirstStop() with
            | None -> Some (calculations.CompletedPositionCostPerShare * TradingStrategyConstants.DefaultStopPriceMultiplier)
            | _ -> calculations.FirstStop()
            
        let numberOfShares =
            match position.StockPositionType with
            | Short -> calculations.CompletedPositionShares * -1m
            | Long -> calculations.CompletedPositionShares
                
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
    
