namespace core.fs.Shared

open System
open System.Collections.Generic
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Stocks

type IApplicationService = interface end

[<CustomEquality>]
[<CustomComparison>]
type StockViolationView =
    {
        CurrentPrice: decimal
        Message: string
        NumberOfShares: decimal
        PricePerShare: decimal
        Ticker: Ticker
    }
    
    override this.Equals(other) =
        match other with
        | :? StockViolationView as res -> res.Ticker = this.Ticker
        | _ -> false
    override this.GetHashCode() = this.Ticker.GetHashCode()
    
    interface IComparable with
        member this.CompareTo(other) =
            match other with
            | :? StockViolationView as res -> this.Ticker.Value.CompareTo(res.Ticker.Value)
            | _ -> -1
   
type ServiceResponse = Ok | Error of ServiceError
// type ServiceResult<'a> = Result<'a, ServiceError>

module ResponseUtils =
            
    let failedTyped<'a> (message: string) =
        ServiceResponse<'a>(ServiceError(message))
        
    let failed (message: string) =
        ServiceError(message) |> Error
        
    let success<'a> (data: 'a) =
        ServiceResponse<'a>(data)
    
    let toOkOrError (response: ServiceResponse<'a>) =
        match response.IsOk with
        | true -> Ok
        | false -> Error response.Error
        
    let toOkOrConcatErrors serviceResponses =
        let failures =
            serviceResponses
            |> Seq.map (fun r ->
                match r with
                | Ok -> None
                | Error serviceError -> Some serviceError
            )
            |> Seq.choose id
        
        match failures |> Seq.isEmpty with
        | true -> Ok
        | false -> failed (failures |> Seq.map (fun r -> r.Message) |> String.concat "\n")
        
module Helpers =
    
    let getViolations brokeragePositions localPositions (prices:Dictionary<string,StockQuote>) =
        
        let brokerageSideViolations =
            brokeragePositions
            |> Seq.map( fun (brokeragePosition:StockPosition) ->
                
                let currentPrice =
                    match prices.TryGetValue(brokeragePosition.Ticker) with
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