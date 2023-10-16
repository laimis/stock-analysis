namespace core.fs.Shared

open System
open core.Shared

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
        
   
type ValueWithFrequency =
    {
        value: decimal
        frequency: int
    }

type LabelWithFrequency =
    {
        label: string
        frequency: int
    }
    
 type ValueFormat =
    | Percentage
    | Currency
    | Number
    | Boolean
    
    with
        
        override this.ToString() =
            match this with
            | Percentage -> nameof Percentage
            | Currency -> nameof Currency
            | Number -> nameof Number
            | Boolean -> nameof Boolean
            
        static member FromString (value: string) =
            match value with
            | nameof Percentage -> Percentage
            | nameof Currency -> Currency
            | nameof Number -> Number
            | nameof Boolean -> Boolean
            | _ -> failwithf $"Unknown value format: %s{value}"