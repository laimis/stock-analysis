module core.fs.Portfolio.MonitoringServices

open System
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Services.Trading
open core.fs.Stocks

type PortfolioAnalysisService(
    accounts: IAccountStorage,
    emails: IEmailService,
    portfolio: IPortfolioStorage,
    brokerage: IBrokerage,
    logger: ILogger) =

    interface IApplicationService
    
    member _.RecentlyClosedPositionUpdates() = task {
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! _ =
            pairs
            |> Seq.map (fun pair -> async {
                let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
                match user with
                | None -> ()
                | Some user ->
                    
                    match user.State.ConnectedToBrokerage with
                    | false -> ()
                    | true ->
                        let! positions = pair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
                        
                        // let positions that were closed in the last 7 days
                        let daysToCheck = 365
                        let maxPositions = 30
                        let recentlyClosedWithoutMaeProcessing =
                            positions
                            |> Seq.filter (_.IsClosed)
                            |> Seq.filter (fun p ->
                                let age = DateTimeOffset.UtcNow.Subtract(p.Closed.Value).TotalDays
                                age >= 0.0 && age <= daysToCheck
                            )
                            |> Seq.filter (fun p -> p.TryGetLabelValue "mae" |> Option.isNone)
                            |> Seq.truncate maxPositions
                            
                        let! _ =
                            recentlyClosedWithoutMaeProcessing
                            |> Seq.map(fun p -> async {
                                
                                let actualTrade = TradingStrategyFactory.createActualTrade()
                                
                                let! bars = brokerage.GetPriceHistory user.State p.Ticker PriceFrequency.Daily (Some p.Opened) (Some p.Closed.Value) |> Async.AwaitTask
                                match bars with
                                | Error e ->
                                    logger.LogError($"Failed to get price history for {p.Ticker} for {p.Opened} to {p.Closed.Value}")
                                | Ok bars ->
                                    let result = actualTrade.Run bars true p
                                    
                                    let tsk =
                                        p
                                        |> StockPosition.setLabel "mae" (result.MaxDrawdownPct.ToString("N2")) DateTimeOffset.UtcNow
                                        |> StockPosition.setLabel "mfe" (result.MaxGainPct.ToString("N2")) DateTimeOffset.UtcNow
                                        |> StockPosition.setLabel "mae10" (result.MaxDrawdownFirst10Bars.ToString("N2")) DateTimeOffset.UtcNow
                                        |> StockPosition.setLabel "mfe10" (result.MaxGainFirst10Bars.ToString("N2")) DateTimeOffset.UtcNow
                                        |> portfolio.SaveStockPosition pair.Id (Some p)
                                        
                                    logger.LogInformation($"Saved MAE/MFE for {p.Ticker} for {p.Opened} to {p.Closed.Value}")
                                    
                                    do! tsk |> Async.AwaitTask
                                
                                ()
                            })
                            |> Async.Sequential
                            
                        ()
                        
            })
            |> Async.Sequential
        
        return ()
    }

    member _.ReportOnThirtyDayTransactions() = task {
        
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! _ =
            pairs
            |> Seq.map (fun pair -> async {
                let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
                match user with
                | None -> ()
                | Some _ ->
                    let! positions = pair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
                    
                    let sellsOfInterest =
                        positions
                        |> Seq.collect _.ShareTransactions
                        |> Seq.filter (fun t ->
                            let agePass =
                                match DateTimeOffset.UtcNow.Subtract(t.Date).TotalDays with
                                | d when d >= 27.0 && d <= 31.0 -> true
                                | _ -> false
                            
                            t.Type = StockTransactionType.Sell && agePass
                        )
                        |> Seq.map (fun t ->
                            {|
                                Ticker = t.Ticker.Value
                                Date = t.Date.ToString "yyyy-MM-dd"
                                Price = t.Price
                                NumberOfShares = t.NumberOfShares
                            |}
                        )
                        
                    if Seq.isEmpty sellsOfInterest |> not then
                        let recipient = Recipient(email = pair.Email, name = "")
                        let! emailResult = emails.SendSellAlert recipient Sender.NoReply {| sells = sellsOfInterest |} |> Async.AwaitTask
                        match emailResult with
                        | Error err ->
                            logger.LogError $"Thirty day sell alert email to {recipient} failed: {err}"
                        | Ok _ ->
                            logger.LogInformation $"Thirty day sell alert email to {recipient} sent successfully"
                        
            })
            |> Async.Sequential
        
        return ()
    }
    
    member _.ReportOnMaxProfitBasedOnDaysHeld() = task {
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! users =
            pairs
            |> Seq.map (fun pair -> async {
                return! pair.Id |> accounts.GetUser |> Async.AwaitTask
            })
            |> Async.Sequential
            
        let usersWithBrokerageConnection = users |> Seq.choose id |> Seq.filter (_.State.ConnectedToBrokerage)
        
        // positions of interest
        let numberOfPositions = 40
        let topDaysOfInterest = 10
        let maxNumberOfDays = 60
        
        let! _ =
            usersWithBrokerageConnection
            |> Seq.map (fun user -> async {
                let! positions = user.State.Id |> UserId |> portfolio.GetStockPositions |> Async.AwaitTask
                
                let lastNPositionsClosed =
                    positions
                    |> Seq.filter (_.IsClosed)
                    |> Seq.sortByDescending (_.Closed.Value)
                    |> Seq.truncate numberOfPositions
                    
                let! positionsAndPriceBarResults =
                    lastNPositionsClosed
                    |> Seq.map (fun p -> async {
                        let start = Some p.Opened
                        let end' = Some (p.Closed.Value.Add(TimeSpan.FromDays(maxNumberOfDays)))
                        
                        let! priceBars = brokerage.GetPriceHistory user.State p.Ticker PriceFrequency.Daily start end' |> Async.AwaitTask
                        return (p, priceBars)
                    })
                    |> Async.Sequential
                    
                // for the actual positions held, get the average days held and the total profit
                let actualPositionsWithCalculations = lastNPositionsClosed |> Seq.map StockPositionWithCalculations
                let actualProfit = Math.Round(actualPositionsWithCalculations |> Seq.sumBy (_.Profit),2)
                let actualDaysHeld = actualPositionsWithCalculations |> Seq.averageBy (fun p -> p.DaysHeld |> decimal) |> int
                    
                let positionsWithPrices =
                    positionsAndPriceBarResults
                    |> Array.map( fun (p, priceBarsResult) ->
                        match priceBarsResult with
                        | Error _ -> None
                        | Ok priceBars -> Some (p, priceBars))
                    |> Array.choose id
                    
                // for each of those position/prices combination, run the analysis
                // what would the profits be from holding for 1 day, 2 days, 3 days, etc.
                let holdPeriods = [ 1 .. maxNumberOfDays ]
                
                let profitsByPeriod =
                    holdPeriods
                    |> List.map (fun days ->
                        
                        let profitForHoldingDay =
                            positionsWithPrices
                            |> Array.map (fun (p:StockPositionState, priceBars:PriceBars) ->
                                
                                // the closing bar should be the number of days from the opened date
                                // sometimes, this will fall on a weekend, so the closing bar should be
                                // the first bar after the number of days
                                let closeBarOption =
                                    priceBars.Bars
                                    |> Array.tryFind (fun b -> b.Date > p.Opened && b.Date >= p.Opened.AddDays(float days))
                                    
                                let closeBar =
                                    match closeBarOption with
                                    | None ->
                                        logger.LogInformation($"No closing bar found for {p.Ticker} after {days} days, {p.Opened.AddDays(float days)}")
                                        priceBars.Last
                                    | Some b -> b
                                    
                                let c = p |> StockPositionWithCalculations
                                let newPosition =
                                    StockPosition.``open`` p.Ticker c.CompletedPositionShares c.CompletedPositionCostPerShare p.Opened
                                    |> StockPosition.sell c.CompletedPositionShares closeBar.Close closeBar.Date
                                    |> StockPositionWithCalculations
                                newPosition.Profit
                            )
                            |> Seq.sum
                            
                        days, profitForHoldingDay
                    )
                 
                let sortedProfitData =
                    profitsByPeriod
                    |> List.sortByDescending snd
                    |> List.truncate topDaysOfInterest
                    |> List.map (fun (days, profit) -> {| days = days; profit = Math.Round(profit, 2) |})
                    
                let maxProfitEntry = profitsByPeriod |> List.maxBy snd
                
                let maxProfit = maxProfitEntry |> snd
                let maxProfitDays = maxProfitEntry |> fst
                let profitData =
                    profitsByPeriod
                    |> List.map (fun (days, profit) ->
                        let percentage = Math.Round((profit / maxProfit) * 100m, 0) |> Math.Abs
                        // negative profit needs a flag so that the email template can use simple if boolean check
                        // to render different style
                        let isNegative = profit < 0m
                        {| days = days; profit = Math.Round(profit, 2); percentage = percentage; isNegative=isNegative |}
                    )
                    
                let actualProfitPercentOfMax = Math.Round(actualProfit / maxProfit * 100m, 0)
                let actualData = {| profit = actualProfit; days = actualDaysHeld; percentage = actualProfitPercentOfMax; numberOfPositions = numberOfPositions |}
                
                let payload =
                    {| sortedProfitData = sortedProfitData; profitData = profitData; actualData = actualData |}

                // // email the user with the max profit and the day it was held
                let recipient = Recipient(email = user.State.Email, name = "")
                let! emailResult = emails.SendMaxProfits recipient Sender.NoReply payload |> Async.AwaitTask
                
                match emailResult with
                | Error err ->
                    logger.LogError $"Max profits email to {recipient} failed: {err}"
                | Ok _ ->
                    logger.LogInformation $"Max profits email to {recipient} sent successfully"
                
                logger.LogInformation $"Max profit for {user.State.Email} is {maxProfit} when held for {maxProfitDays} days"
                ()
                
            })
            |> Async.Sequential
        
        return ()
    }
