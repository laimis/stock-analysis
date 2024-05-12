namespace core.fs.Portfolio

open System.Collections.Generic
open core.Shared
open core.Stocks
open core.fs.Adapters.Brokerage
open core.fs.Stocks

module Helpers =

    let getViolations (account:BrokerageAccount) localPositions (pendingPositions:PendingStockPosition seq) (prices:Dictionary<Ticker,StockQuote>) =
        
        match account.Connected with
        | false -> Seq.empty
        | true ->
        let brokerageSideViolations =
            account.StockPositions
            |> Seq.map( fun (brokeragePosition:StockPosition) ->
                
                let currentPrice =
                    match prices.TryGetValue(brokeragePosition.Ticker) with
                    | true, price -> price.Price
                    | false, _ -> 0m
                    
                let localPositionOption = localPositions |> Seq.tryFind (fun (x:StockPositionWithCalculations) -> x.Ticker = brokeragePosition.Ticker)
                let localPendingPositionOption =
                    pendingPositions
                    |> Seq.tryFind (fun (x:PendingStockPosition) -> x.State.Ticker = brokeragePosition.Ticker)
                    |> Option.map _.State
                
                match localPositionOption with
                | None ->
                    let violation = {
                            CurrentPrice = currentPrice
                            Message = $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost:F2} but NGTrading says none"
                            NumberOfShares = brokeragePosition.Quantity
                            PricePerShare = brokeragePosition.AverageCost
                            Ticker = brokeragePosition.Ticker
                            LocalPosition = None
                            PendingPosition = localPendingPositionOption
                        }
                    Some violation
                    
                | Some localPosition ->
                    match localPosition.NumberOfShares = brokeragePosition.Quantity with
                    | true -> None
                    | false ->
                        let violation = {
                                CurrentPrice = currentPrice
                                Message = $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost:F2} but NGTrading says {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare:F2}"
                                NumberOfShares = brokeragePosition.Quantity
                                PricePerShare = brokeragePosition.AverageCost
                                Ticker = brokeragePosition.Ticker
                                LocalPosition = localPositionOption
                                PendingPosition = localPendingPositionOption
                            }
                        Some violation
                )
            
        let localSideViolations =
            localPositions
            |> Seq.map (fun localPosition ->
                
                let currentPrice =
                    match prices.TryGetValue(localPosition.Ticker) with
                    | true, price -> price.Price
                    | false, _ -> 0m
                    
                let brokeragePositionOption = account.OptionPositions |> Seq.tryFind (fun x -> x.Ticker.Value = localPosition.Ticker)
                
                match brokeragePositionOption with
                | None -> 
                    let violation = {
                            CurrentPrice = currentPrice
                            Message = $"Owned {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare:F2} but brokerage says none"
                            NumberOfShares = localPosition.NumberOfShares
                            PricePerShare = localPosition.AverageCostPerShare
                            Ticker = localPosition.Ticker
                            LocalPosition = localPosition |> Some
                            PendingPosition = None 
                        }
                    Some violation
                    
                | Some brokeragePosition ->
                    
                    match brokeragePosition.Quantity = localPosition.NumberOfShares with
                    | true -> None
                    | false ->
                        let violation = {
                                CurrentPrice = currentPrice
                                Message = $"Owned {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare:F2} but brokerage says {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost:F2}"
                                NumberOfShares = localPosition.NumberOfShares
                                PricePerShare = localPosition.AverageCostPerShare
                                Ticker = localPosition.Ticker
                                LocalPosition = localPosition |> Some
                                PendingPosition = None 
                            }
                        Some violation
            )
            
        brokerageSideViolations |> Seq.append localSideViolations |> Seq.choose id |> Seq.distinct
