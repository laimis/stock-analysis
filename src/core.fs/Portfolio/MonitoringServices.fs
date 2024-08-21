module core.fs.Portfolio.MonitoringServices

open System
open System.Threading
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Stocks

type PortfolioAnalysisService(
    accounts: IAccountStorage,
    emails: IEmailService,
    portfolio: IPortfolioStorage,
    brokerage: IBrokerage,
    logger: ILogger) =

    interface IApplicationService

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
                                Date = t.Date
                                Price = t.Price
                                NUmberOfShares = t.NumberOfShares
                            |}
                        )
                        
                    if Seq.isEmpty sellsOfInterest |> not then
                        let recipient = Recipient(email = pair.Email, name = "")
                        do! emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.SellAlert {| sells = sellsOfInterest |} |> Async.AwaitTask
                        
            })
            |> Async.Parallel
        
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
        
        let numberOfPositions = 40
        
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
                        let end' = Some (p.Closed.Value.Add(TimeSpan.FromDays(60.0)))
                        
                        logger.LogInformation($"Getting price history for {p.Ticker} from {start.Value} to {end'.Value}")
                        
                        let! priceBars = brokerage.GetPriceHistory user.State p.Ticker PriceFrequency.Daily (Some p.Opened) (Some (p.Closed.Value.Add(TimeSpan.FromDays(60.0)))) |> Async.AwaitTask
                        return (p, priceBars)
                    })
                    |> Async.Sequential
                    
                let positionsWithPrices =
                    positionsAndPriceBarResults
                    |> Array.map( fun (p, priceBarsResult) ->
                        match priceBarsResult with
                        | Error _ -> None
                        | Ok priceBars -> Some (p, priceBars))
                    |> Array.choose id
                    
                // for each of those position/prices combination, run the analysis
                // what would the profits be from holding for 1 day, 2 days, 3 days, etc.
                let holdPeriods = [ 1 .. 60 ]
                
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
                                    StockPosition.``open`` p.Ticker c.CompletedPositionShares c.FirstBuyPrice p.Opened
                                    |> StockPosition.sell c.CompletedPositionShares closeBar.Close closeBar.Date
                                    |> StockPositionWithCalculations
                                newPosition.Profit
                            )
                            |> Seq.sum
                            
                        (days, profitForHoldingDay)
                    )
                    
                // find the max profit and the day it was held
                profitsByPeriod |> List.sortByDescending snd |> List.iter (fun (days, profit) ->
                    logger.LogInformation($"Max profit for {user.State.Email} is {profit} when held for {days} days")
                )
                
                let sortedProfitData =
                    profitsByPeriod
                    |> List.sortByDescending snd
                    |> List.truncate 10
                    |> List.map (fun (days, profit) -> {| days = days; profit = profit |})
                    
                let maxProfit = profitsByPeriod |> List.maxBy snd |> snd
                
                let profitData =
                    profitsByPeriod
                    |> List.map (fun (days, profit) ->
                        let percentage = Math.Round((profit / maxProfit) * 100m, 0)
                        {| days = days; profit = profit; percentage = percentage |}
                    )
                
                let payload =
                    {| sortedProfitData = sortedProfitData; profitData = profitData |}

                // // email the user with the max profit and the day it was held
                let recipient = Recipient(email = user.State.Email, name = "")
                do! emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.MaxProfits payload |> Async.AwaitTask
                
                logger.LogInformation($"Max profit for {user.State.Email} is {maxProfit}")
                ()
                
            })
            |> Async.Parallel
        
        return ()
    }
