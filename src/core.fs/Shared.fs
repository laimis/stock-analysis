namespace core.fs

open System
open System.Collections.Generic


type IApplicationService = interface end
   
   
type ServiceError(message:string) =
    member this.Message = message

module CultureUtils =
    let DefaultCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US")
    
module ResponseUtils =
            
    let toOkOrConcatErrors serviceResponses : Result<Unit,ServiceError> =
        let failures =
            serviceResponses
            |> Seq.map (fun (r:Result<unit,ServiceError>) ->
                match r with
                | Ok _ -> None
                | Error err -> Some err
            )
            |> Seq.choose id
        
        match failures |> Seq.isEmpty with
        | true -> Ok ()
        | false -> (failures |> Seq.map _.Message |> String.concat "\n") |> ServiceError |> Error
        
   
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

type SentimentType =
    | Negative
    | Neutral
    | Positive
    
    static member FromString(value:string) =
        match value with
        | nameof Negative -> Negative
        | nameof Neutral -> Neutral
        | nameof Positive -> Positive
        | _ -> failwith $"Invalid gap type: {value}"
        
    override this.ToString() =
        match this with
        | Negative -> nameof Negative
        | Neutral -> nameof Neutral
        | Positive -> nameof Positive
    

            
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
    | Scatter
    
    with 
    
        override this.ToString() =
            match this with
            | Line -> nameof Line
            | Column -> nameof Column
            | Scatter -> nameof Scatter
            
        static member FromString (value: string) =
            match value with
            | nameof Line -> Line
            | nameof Column -> Column
            | nameof Scatter -> Scatter
            | _ -> failwithf $"Unknown data point chart type: %s{value}"
            
type ChartAnnotationLine(value:decimal,chartAnnotationLineType:ChartAnnotationLineType) =
    member this.Value = value
    member this.ChartAnnotationLineType = chartAnnotationLineType

type DataPoint<'a>(label:string,value:'a,isDate:bool,ticker:string option) =
    
    new(label:string,value:'a) =
        DataPoint(label,value,isDate = false,ticker = None)
    
    new(label:string,value:'a,ticker:string) =
        DataPoint(label,value,isDate = false,ticker = Some ticker)
        
    new(timestamp:DateTimeOffset,value:'a) =
        DataPoint(timestamp.ToString("yyyy-MM-dd"),value,isDate = true,ticker = None)
        
    member this.Label = label
    member this.Value = value
    member this.Ticker = ticker
    member this.IsDate = isDate
    
    override this.ToString() = $"{label}: {value}"
    
type ChartDataPointContainer<'a>(label:string,chartType:DataPointChartType,annotationLine:ChartAnnotationLine option) =
    
    let data = List<DataPoint<'a>>()
    
    new (label:string,chartType:DataPointChartType) =
        ChartDataPointContainer(label,chartType,None)
    
    member this.Label = label
    member this.ChartType = chartType
    member this.Data = data
    member this.AnnotationLine = annotationLine
    
    member this.Add(label:string,value:'a) =
        data.Add(DataPoint(label,value))
        this
        
    member this.Add(label:string,value:'a,ticker:string) =
        data.Add(DataPoint(label,value,ticker))
        this
        
    member this.Add(timestamp:DateTimeOffset,value:'a) =
        data.Add(DataPoint(timestamp,value))
    member this.Add(point) = data.Add(point)
