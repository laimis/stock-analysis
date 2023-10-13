namespace core.fs.Services.Trading

open System
open core.Account
open core.Shared
open core.Shared.Adapters.Stocks
open core.Stocks
open core.Stocks.Services.Trading
open core.fs.Shared.Adapters.Brokerage


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
            
            match prices.IsOk with
            | false ->
                results.MarkAsFailed($"Failed to get price history for {ticker}: {prices.Error.Message}")
            | true ->
                let bars = prices.Success
                match bars with
                | [||] -> results.MarkAsFailed($"No price history found for {ticker}")
                | _ ->
                    // HACK: sometimes stock is purchased in after hours at a much higher or lower price
                    // than what the day's high/close was, we need to move the prices to the next day
                    let bars =
                        match price > bars[0].High with
                        | true -> bars[1..]
                        | false -> bars
                    
                    TradingStrategyFactory.GetStrategies()
                    |> Seq.iter ( fun strategy ->
                        let positionInstance = PositionInstance(0, ticker, ``when``)
                        positionInstance.Buy(numberOfShares, price, ``when``, Guid.NewGuid())
                        positionInstance.SetStopPrice(stopPrice, ``when``)
                        
                        let result = strategy.Run(
                            position = positionInstance,
                            bars = bars
                        )
                        
                        if closeIfOpenAtTheEnd && not result.position.IsClosed then
                            result.position.Sell(
                                numberOfShares = result.position.NumberOfShares,
                                price = bars[bars.Length - 1].Close,
                                transactionId = Guid.NewGuid(),
                                ``when`` = bars[bars.Length - 1].Date
                            )
                            
                        results.Add(result)
                    )
                    
                    match actualTrade with
                    | Some actualTrade ->
                        let actualResult = TradingStrategyFactory.CreateActualTrade().Run(actualTrade, bars)
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