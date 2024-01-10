module core.fs.Portfolio.MonitoringServices

open System
open System.Threading
open core.fs
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Stocks

type ThirtyDaySellService(
    accounts: IAccountStorage,
    emails: IEmailService,
    portfolio: IPortfolioStorage) =

    interface IApplicationService

    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {
        
        let! pairs = accounts.GetUserEmailIdPairs()
        
        let! _ =
            pairs
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (fun pair -> async {
                let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
                match user with
                | None -> ()
                | Some user ->
                    let! positions = pair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
                    
                    let sellsOfInterest =
                        positions
                        |> Seq.filter _.IsOpen
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
                                Ticker = t.Ticker
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
    
    member _.NextRun (now:DateTimeOffset) = now.AddHours(24.0)