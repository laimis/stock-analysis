namespace core.fs.Shared.Adapters.Stocks

open System
open System.Collections.Generic

[<CLIMutable>]
type StockProfile = {
    Symbol: string
    Description: string
    SecurityName: string
    IssueType: string
    Exchange: string
    Cusip: string
    Fundamentals: Dictionary<string, string>
}

type PriceFrequency =
    | Daily
    | Weekly
    | Monthly
    
    with
    
        static member FromString (value: string) =
            match value with
            | "daily" -> Daily
            | "weekly" -> Weekly
            | "monthly" -> Monthly
            | _ -> failwith "Invalid PriceFrequency"
        
        override this.ToString () =
            
            match this with
            | Daily -> "daily"
            | Weekly -> "weekly"
            | Monthly -> "monthly"
            
type PriceBar(date:DateTimeOffset, ``open``:decimal, high:decimal, low:decimal, close:decimal, volume:int64) =
    
    member this.Date = date
    member this.Open = ``open``
    member this.High = high
    member this.Low = low
    member this.Close = close
    member this.Volume = volume
    
    member this.DateStr = this.Date.ToString("yyyy-MM-dd")
    
    override this.ToString () =
        $"{this.DateStr},{this.Open},{this.High},{this.Low},{this.Close},{this.Volume}"
        
    override this.Equals (obj:obj) =
        match obj with
        | :? PriceBar as other -> this.DateStr = other.DateStr
        | _ -> false
        
    override this.GetHashCode () =
        this.DateStr.GetHashCode ()
    
    static member Parse (value:string) =
        let parts = value.Split(',')
        PriceBar(
            date = DateTimeOffset.Parse(parts[0]),
            ``open`` = Decimal.Parse(parts[1]),
            high = Decimal.Parse(parts[2]),
            low = Decimal.Parse(parts[3]),
            close = Decimal.Parse(parts[4]),
            volume = Int64.Parse(parts[5])
        )
    
    member this.PercentDifferenceFromLow (value:decimal) =
        (this.Low - value) / value
    
    member this.PercentDifferenceFromHigh (value:decimal) =
        (this.High - value) / value
        
    member this.ClosingRange() =
        
        let rangeDenominator = this.High - this.Low
        match rangeDenominator with
        | 0m -> 0m
        | _ -> (this.Close - this.Low) / rangeDenominator