namespace core.fs.Shared.Adapters.Stocks

open System
open System.Collections.Generic
open System.Globalization

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
    
    static member FromString (value: string) =
        match value with
        | nameof Daily -> Daily
        | nameof Weekly -> Weekly
        | nameof Monthly -> Monthly
        | _ -> failwith $"Invalid PriceFrequency: {value}"
    
    override this.ToString () =
        
        match this with
        | Daily -> nameof Daily
        | Weekly -> nameof Weekly
        | Monthly -> nameof Monthly
            
type PriceBar(date:DateTimeOffset, ``open``:decimal, high:decimal, low:decimal, close:decimal, volume:int64) =
    
    new(value:string) =
        let parts = value.Split(',')
        PriceBar(
            date = DateTimeOffset.Parse(parts[0], formatProvider=null, styles=DateTimeStyles.AssumeUniversal),
            ``open`` = Decimal.Parse(parts[1]),
            high = Decimal.Parse(parts[2]),
            low = Decimal.Parse(parts[3]),
            close = Decimal.Parse(parts[4]),
            volume = Int64.Parse(parts[5])
        )
        
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
    
    member this.PercentDifferenceFromLow (value:decimal) =
        (this.Low - value) / value
    
    member this.PercentDifferenceFromHigh (value:decimal) =
        (this.High - value) / value
        
    member this.ClosingRange() =
        
        let rangeDenominator = this.High - this.Low
        match rangeDenominator with
        | 0m -> 0m
        | _ -> (this.Close - this.Low) / rangeDenominator
        
type PriceBars(bars:PriceBar array) =
    member this.Bars = bars
    member this.Length = bars.Length
    member this.First = bars[0]
    member this.Last = bars[this.Length - 1]
    member this.LatestOrAll numberOfBars =
        match numberOfBars > this.Length with
        | true -> this.Bars
        | false -> this.Bars[this.Length - numberOfBars ..]
        |> PriceBars
    member this.AllButLast = this.Bars[0 .. this.Length - 2] |> PriceBars
    member this.ClosingPrices() = this.Bars |> Array.map (fun bar -> bar.Close)
    member this.Volumes() = this.Bars |> Array.map (fun bar -> bar.Volume |> decimal) 