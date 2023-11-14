namespace core.fs.Shared

open System
open System.Collections.Generic
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
   
   
type ServiceError(message:string) =
    member this.Message = message
    
type ServiceResponse =
    | Ok
    | Error of ServiceError
    
type ServiceResponse<'a>(success:'a option,error:ServiceError option) =
    
    new(success:'a) =
        ServiceResponse<'a>(success = Some(success),error = None)
    
    new(error:ServiceError) =
        ServiceResponse<'a>(success = None,error = Some(error))
        
    member this.Success = success
    member this.Error = error
    member this.IsOk = match this.Success with | Some _ -> true | None -> false
    member this.Result =
        match this.Success with
        | Some s -> Result.Ok s
        | None -> Result.Error this.Error.Value
    
module ResponseUtils =
            
    let failedTyped<'a> (message: string) =
        message |> ServiceError |> ServiceResponse<'a>
        
    let failed (message: string) : ServiceResponse =
        message |> ServiceError |> Error
        
    let success<'a> (data: 'a) =
        ServiceResponse<'a>(data)
    
    let toOkOrError (response: ServiceResponse<'a>) : ServiceResponse =
        match response.IsOk with
        | true -> Ok
        | false -> Error response.Error.Value
        
    let toOkOrConcatErrors serviceResponses : ServiceResponse =
        let failures =
            serviceResponses
            |> Seq.map (fun (r:ServiceResponse) ->
                match r with
                | Ok -> None
                | Error err -> Some err
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
            
type ChartAnnotationLineType =
    | Vertical
    | Horizontal
    
    with
    
        override this.ToString() =
            match this with
            | Vertical -> nameof Vertical
            | Horizontal -> nameof Horizontal
            
        static member FromString (value: string) =
            match value with
            | nameof Vertical -> Vertical
            | nameof Horizontal -> Horizontal
            | _ -> failwithf $"Unknown chart annotation line type: %s{value}"

type DataPointChartType =
    | Line
    | Column
    
    with 
    
        override this.ToString() =
            match this with
            | Line -> nameof Line
            | Column -> nameof Column
            
        static member FromString (value: string) =
            match value with
            | nameof Line -> Line
            | nameof Column -> Column
            | _ -> failwithf $"Unknown data point chart type: %s{value}"
            
type ChartAnnotationLine(value:decimal,chartAnnotationLineType:ChartAnnotationLineType) =
    member this.Value = value
    member this.ChartAnnotationLineType = chartAnnotationLineType

type DataPoint<'a>(label:string,value:'a,isDate:bool) =
    
    new(label:string,value:'a) =
        DataPoint(label,value,isDate = false)
        
    new(timestamp:DateTimeOffset,value:'a) =
        DataPoint(timestamp.ToString("yyyy-MM-dd"),value,isDate = true)
        
    member this.Label = label
    member this.Value = value
    member this.IsDate = isDate
    
type ChartDataPointContainer<'a>(label:string,chartType:DataPointChartType,annotationLine:ChartAnnotationLine option) =
    
    let data = List<DataPoint<'a>>()
    
    new (label:string,chartType:DataPointChartType) =
        ChartDataPointContainer(label,chartType,None)
    
    member this.Label = label
    member this.ChartType = chartType
    member this.Data = data
    member this.AnnotationLine = annotationLine
    
    member this.Add(label:string,value:'a) =
        data.Add(DataPoint(label,value,isDate = false))
        
    member this.Add(timestamp:DateTimeOffset,value:'a) =
        data.Add(DataPoint(timestamp,value))
    member this.Add(point) = data.Add(point)