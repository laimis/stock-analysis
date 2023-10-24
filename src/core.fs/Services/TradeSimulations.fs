namespace core.fs.Services.TradingStrategies

open System
open System.Collections.Generic
open System.Security.Cryptography
open core.Account
open core.Shared
open core.Stocks
open core.fs.Services.Trading
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks

[<AbstractClass>]
type TradingStrategy(name:string) =
    
    let mutable _numberOfSharesAtStart = 0m
    
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
    
    abstract member ApplyPriceBarToPositionInternal : SimulationContext -> PriceBar -> unit
    
    member this.ApplyPriceBarToPosition (context:SimulationContext) (bar:PriceBar) =
        
        let position = context.Position
        
        if position.IsClosed then
            context
        else
            position.SetPrice(bar.Close)
            
            this.ApplyPriceBarToPositionInternal context bar
            
            let last10bars = context.Last10Bars
            
            if last10bars.Count = 10 then
                last10bars.RemoveAt(0)
                
            last10bars.Add(bar)
            
            {
                context with
                    Position = position
                    MaxDrawdown = Math.Min(context.MaxDrawdown,bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare))
                    MaxGain = Math.Max(context.MaxGain,bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare))
                    Last10Bars = last10bars
            }
    
    member this.ClosePosition price date (position:PositionInstance) =
        if position.NumberOfShares > 0m then
            position.Sell(
                numberOfShares = position.NumberOfShares,
                price = price,
                transactionId = Guid.NewGuid(),
                ``when`` = date
            )

    interface  ITradingStrategy with
    
        member this.Run (position:PositionInstance) (bars:seq<PriceBar>) =
            
            let context = 
                {
                    Position = position
                    MaxDrawdown = 0m
                    MaxGain = 0m
                    Last10Bars = List<PriceBar>(10)
                }
                
            _numberOfSharesAtStart <- position.NumberOfShares
                
            let finalContext =
                bars
                |> Seq.fold this.ApplyPriceBarToPosition context
                
            let maxDrawdownPctRecent,maxGainPctRecent = TradingStrategy.CalculateMaxDrawdownAndGain finalContext.Last10Bars
            
            {
                MaxDrawdownPct = finalContext.MaxDrawdown
                MaxGainPct = finalContext.MaxGain
                MaxDrawdownPctRecent = maxDrawdownPctRecent
                MaxGainPctRecent = maxGainPctRecent
                Position = finalContext.Position
                StrategyName = this.Name
            }

type TradingStrategyCloseOnCondition(name:string,exitCondition) =
    
    inherit TradingStrategy(name)
    
    override this.ApplyPriceBarToPositionInternal (context:SimulationContext) (bar:PriceBar) =
        if exitCondition context bar then
            this.ClosePosition bar.Close bar.Date context.Position
            

type TradingStrategyActualTrade() =
    
    interface ITradingStrategy with
    
        member this.Run (position:PositionInstance) (bars:seq<PriceBar>) =
            
            let finalPosition, maxDrawdownPct, maxGainPct, last10Bars =
                bars
                |> Seq.fold (fun (position:PositionInstance, maxDrawdownPct, maxGainPct, last10Bars:PriceBar list) bar ->
                    if position.IsClosed && bar.Date.Date = position.Closed.Value.Date then
                        position, maxDrawdownPct, maxGainPct, last10Bars
                    else
                        position.SetPrice(bar.Close)
                        
                        let maxDrawdownPct = Math.Min(maxDrawdownPct,bar.PercentDifferenceFromLow(position.AverageBuyCostPerShare))
                        let maxGainPct = Math.Max(maxGainPct,bar.PercentDifferenceFromHigh(position.AverageBuyCostPerShare))
                        
                        let newLast10Bars =
                            match last10Bars.Length with
                            | x when x = 10 -> last10Bars[1..9] @ [bar]
                            | _ -> last10Bars @ [bar]
                            
                        position, maxDrawdownPct, maxGainPct, newLast10Bars  
                ) (position, Decimal.MaxValue, Decimal.MinValue, [])
                
            let maxDrawdownPctRecent,maxGainPctRecent = TradingStrategy.CalculateMaxDrawdownAndGain(last10Bars)
                
            {
                MaxDrawdownPct = maxDrawdownPct
                MaxGainPct = maxGainPct
                MaxDrawdownPctRecent = maxDrawdownPctRecent
                MaxGainPctRecent = maxGainPctRecent
                Position = finalPosition
                StrategyName = TradingStrategyConstants.ActualTradesName
            }

// type TradingStrategyWithAdvancingStops(name:string,profitPointFunc,stopPriceFunc) =
//     
//     inherit TradingStrategy(name)
//     
//     override this.ApplyPriceBarToPositionInternal context bar =
//         
//         let profitPoint = profitPointFunc context.Position
//         
//         if bar.High > profitPoint then
//             context.Position.SetStopPrice(stopPriceFunc context.Position,bar.Date)
//             
//         if bar.Close <= context.Position.StopPrice.Value then
//             this.ClosePosition bar.Close bar.Date context.Position


// type TradingStrategyWithDownsideProtection(name:string,profitPointFunc,stopPriceFunc,downsideProtectionSize) =
//     
//     inherit TradingStrategy(name)
//     
//     let mutable _executed = false
//     let mutable _level = 0
//     
//     override this.ApplyPriceBarToPositionInternal(context:SimulationContext,bar:PriceBar) =
//         
//         let profitPoint = profitPointFunc  context.Position _level
//         
//         if bar.High > profitPoint then
//             context.Position.SetStopPrice(stopPriceFunc context.Position,bar.Date)
//             _level <- _level + 1
//             
//         if not _executed && context.Position.RR < -0.5m && context.Position.NumberOfShares > 0m then
//             let stocksToSell = int (context.Position.NumberOfShares / downsideProtectionSize)
//             if stocksToSell > 0 then
//                 context.Position.Sell(stocksToSell,bar.Close,Guid.NewGuid(),bar.Date)
//                 _executed <- true
//                 

type TradingStrategyWithProfitPoints(name:string,numberOfProfitPoints,profitPointFunc,stopPriceFunc) =
    
    inherit TradingStrategy(name)
    
    let mutable _level = 1
    
    member this.ExecuteProfitSell (position:PositionInstance) sellPrice (bar:PriceBar) =
        
        // figure out how much to sell based on the number of profit points
        // and how many shares we have left
        let portion =
            if _level = numberOfProfitPoints then
                position.NumberOfShares
            else
                match int (this.NumberOfSharesAtStart / decimal numberOfProfitPoints) |> decimal with
                | 0m -> 1m
                | x when x > position.NumberOfShares -> position.NumberOfShares    
                | x -> x
            
        position.Sell(
            numberOfShares = portion,
            price = sellPrice,
            transactionId = Guid.NewGuid(),
            ``when`` = bar.Date
        )
        
        if position.NumberOfShares > 0m then
            let stopPrice:decimal = stopPriceFunc position _level
            position.SetStopPrice(
                stopPrice,
                bar.Date
            )
            
    override this.ApplyPriceBarToPositionInternal context bar =
        
        let sellPrice = profitPointFunc context.Position _level
        
        if bar.High >= sellPrice then
            this.ExecuteProfitSell context.Position sellPrice bar
            _level <- _level + 1
            
        if bar.Close <= context.Position.StopPrice.Value then
            this.ClosePosition bar.Close bar.Date context.Position
            
            
module TradingStrategyFactory =
    
    let advancingStop (level:int) (position:PositionInstance) rrFunc =
        match level with
        | 1 -> position.AverageCostPerShare
        | _ -> rrFunc position (level - 1)
    
    let delayedAdvancingStop (level:int) (position:PositionInstance) (rrLevelFunc:int -> decimal) =
        match level with
        | 1 -> position.StopPrice.Value
        | 2 -> position.AverageCostPerShare
        | _ -> rrLevelFunc (level - 2)
    
    let createActualTrade() : ITradingStrategy = TradingStrategyActualTrade()
    let createProfitPointsTrade numberOfProfitPoints : ITradingStrategy =
        let stopFunc = fun (position:PositionInstance) (level:int) -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
        TradingStrategyWithProfitPoints($"Profit points: {numberOfProfitPoints}", numberOfProfitPoints, ProfitPoints.getProfitPointWithStopPrice, stopFunc)
    
    let createProfitPointsBasedOnPctGainTrade percentGain numberOfProfitPoints : ITradingStrategy =
        let profitPointFunc = fun (position:PositionInstance) (level:int) -> ProfitPoints.getProfitPointWithPercentGain position level percentGain
        let stopFunc = fun (position:PositionInstance) (level:int) -> advancingStop level position ProfitPoints.getProfitPointWithStopPrice
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
    
type TradingStrategyRunner(brokerage:IBrokerage, hours:IMarketHours) =
    
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice:decimal,
            ticker:Ticker,
            ``when``:DateTimeOffset,
            closeIfOpenAtTheEnd:bool,
            actualTrade:PositionInstance option) =
        
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
                match bars with
                | [||] -> results.MarkAsFailed($"No price history found for {ticker}")
                | _ ->
                    
                    TradingStrategyFactory.getStrategies()
                    |> Seq.iter ( fun strategy ->
                        let positionInstance = PositionInstance(0, ticker, ``when``)
                        positionInstance.Buy(numberOfShares, price, ``when``, Guid.NewGuid())
                        positionInstance.SetStopPrice(stopPrice, ``when``)
                        
                        let result = strategy.Run positionInstance bars
                        
                        if closeIfOpenAtTheEnd && not result.Position.IsClosed then
                            result.Position.Sell(
                                numberOfShares = result.Position.NumberOfShares,
                                price = bars[bars.Length - 1].Close,
                                transactionId = Guid.NewGuid(),
                                ``when`` = bars[bars.Length - 1].Date
                            )
                            
                        results.Add(result)
                    )
                    
                    match actualTrade with
                    | Some actualTrade ->
                        let actualResult = TradingStrategyFactory.createActualTrade().Run actualTrade bars
                        results.Insert(0, actualResult)
                    | None -> ()
            
            
            return results
        }
        
    member this.Run(
            user:UserState,
            numberOfShares:decimal,
            price:decimal,
            stopPrice:decimal,
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
        
    member this.Run(user:UserState, position:PositionInstance,closeIfOpenAtTheEnd) =
        
        let stopPrice =
            if position.FirstStop.HasValue then
                position.FirstStop.Value
            else
                position.CompletedPositionCostPerShare * TradingStrategyConstants.DefaultStopPriceMultiplier
                
        this.Run(
            user=user,
            numberOfShares=position.CompletedPositionShares,
            price=position.CompletedPositionCostPerShare,
            stopPrice = stopPrice,
            ticker = position.Ticker,
            ``when``=position.Opened,
            closeIfOpenAtTheEnd=closeIfOpenAtTheEnd,
            actualTrade = Some position
        )
    