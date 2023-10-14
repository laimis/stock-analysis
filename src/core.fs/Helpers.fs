namespace core.fs

open System.Collections.Generic
open core.Stocks
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage

module Helpers =

    let getViolations brokeragePositions localPositions (prices:Dictionary<string,StockQuote>) =
        
        let brokerageSideViolations =
            brokeragePositions
            |> Seq.map( fun (brokeragePosition:StockPosition) ->
                
                let currentPrice =
                    match prices.TryGetValue(brokeragePosition.Ticker.Value) with
                    | true, price -> price.Price
                    | false, _ -> 0m
                    
                let localPositionOption = localPositions |> Seq.tryFind (fun (x:PositionInstance) -> x.Ticker = brokeragePosition.Ticker)
                
                match localPositionOption with
                | None ->
                    let violation = {
                            CurrentPrice = currentPrice
                            Message = $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says none"
                            NumberOfShares = brokeragePosition.Quantity
                            PricePerShare = brokeragePosition.AverageCost
                            Ticker = brokeragePosition.Ticker
                        }
                    Some violation
                    
                | Some localPosition ->
                    match localPosition.NumberOfShares = brokeragePosition.Quantity with
                    | true -> None
                    | false ->
                        let violation = {
                                CurrentPrice = currentPrice
                                Message = $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare}"
                                NumberOfShares = brokeragePosition.Quantity
                                PricePerShare = brokeragePosition.AverageCost
                                Ticker = brokeragePosition.Ticker
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
                    
                let brokeragePositionOption = brokeragePositions |> Seq.tryFind (fun x -> x.Ticker = localPosition.Ticker)
                
                match brokeragePositionOption with
                | None -> 
                    let violation = {
                            CurrentPrice = currentPrice
                            Message = $"Owned {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare} but brokerage says none"
                            NumberOfShares = localPosition.NumberOfShares
                            PricePerShare = localPosition.AverageCostPerShare
                            Ticker = localPosition.Ticker
                        }
                    Some violation
                    
                | Some brokeragePosition ->
                    
                    match brokeragePosition.Quantity = localPosition.NumberOfShares with
                    | true -> None
                    | false ->
                        let violation = {
                                CurrentPrice = currentPrice
                                Message = $"Owned {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare} but brokerage says {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost}"
                                NumberOfShares = localPosition.NumberOfShares
                                PricePerShare = localPosition.AverageCostPerShare
                                Ticker = localPosition.Ticker
                            }
                        Some violation
            )
            
        brokerageSideViolations |> Seq.append localSideViolations |> Seq.choose id |> Seq.distinct