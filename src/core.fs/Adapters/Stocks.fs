namespace core.fs.Adapters.Stocks

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
    
    
    let validBar = ``open`` <= high && ``open`` >= low
    
    do
        // check that none of the values are negative
        if ``open`` < 0m then failwith $"Invalid open price: {``open``} on {date}"
        if high < 0m then failwith $"Invalid high price: {high}"
        if low < 0m then failwith $"Invalid low price: {low}"
        if close < 0m then failwith $"Invalid close price: {close}"
        if volume < 0L then failwith $"Invalid volume: {volume}"
        
        // check that the high is greater than or equal to the low
        if high < low then failwith $"Invalid high/low prices: {high}/{low}"
        
        // TODO: uncomment these checks when we have a better data source
        // right now I see this happen in TD Ameritrade feed
        // // check that the close is between the high and low
        // if close > high then failwith $"Invalid close price: {close} is greater than high: {high} on {date}"
        // if close < low then failwith $"Invalid close price: {close} is less than low: {low} on {date}"
        //
        // // check that the open is between the high and low
        // if ``open`` > high then failwith $"Invalid open price: {``open``} is greater than high: {high} on {date}"
        // if ``open`` < low then failwith $"Invalid open price: {``open``} is less than low: {low} on {date}"
    
    let dateStr = date.ToString("yyyy-MM-dd")
    
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
    
    member this.DateStr = dateStr
    
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
        
    member this.percentDifferenceFromClose (value:decimal) =
        (this.Close - value) / value
    
    member this.PercentDifferenceFromHigh (value:decimal) =
        (this.High - value) / value
        
    member this.ClosingRange() =
        
        let rangeDenominator = this.High - this.Low
        match rangeDenominator with
        | 0m -> 0m
        | _ -> (this.Close - this.Low) / rangeDenominator
        
    member this.TrueRange (previousBar:PriceBar option) =
        let candidates =
            [
                high - low
                if previousBar.IsSome then abs (high - previousBar.Value.Close) else 0m
                if previousBar.IsSome then abs (low - previousBar.Value.Close) else 0m
            ]
            
        candidates |> List.max
        
    member this.TrueRangePercentage (previousBar:PriceBar option) =
        this.TrueRange(previousBar) / this.Open
        
    member this.HasGap (referenceBar:PriceBar) =
        // needed to add a check for valid bar here otherwise the gap condition
        // will evaluate to true, but when calculating the gap will come back with nonsensical
        // result because the bar itself is not valid. This due to data provider issues, take
        // a look at the constructor
        validBar && (this.Low > referenceBar.High || this.High < referenceBar.Low)
        
type PriceBarWithIndex = int * PriceBar

type PriceBars(bars:PriceBar array, frequency:PriceFrequency) =
    let dateIndex = Dictionary<string, PriceBarWithIndex>()
    let barsWithIndex = bars |> Array.indexed
    
    do
        barsWithIndex
        |> Array.iter (fun indexWithBar -> dateIndex.Add(indexWithBar |> snd |> _.DateStr, indexWithBar))
    
    new (bars:PriceBar array) = PriceBars(bars, PriceFrequency.Daily)
            
    member this.Bars = bars
    member this.BarsWithIndex = barsWithIndex
    member this.Frequency = frequency
    member this.Length = bars.Length
    member this.First = bars[0]
    member this.Last = bars[this.Length - 1]
    member this.LatestOrAll numberOfBars = PriceBars(this.Bars[^(numberOfBars-1)..], frequency)
    member this.AllButLast() = PriceBars(this.Bars[0 .. this.Length - 2], frequency)
    member this.ClosingPrices() = this.Bars |> Array.map (_.Close)
    member this.Volumes() = this.Bars |> Array.map (fun bar -> bar.Volume |> decimal)
    member this.TryFindByDate (dateStr:string) =
        match dateIndex.TryGetValue(dateStr) with
        | true, bar -> Some bar
        | _ -> None
    member this.TryFindByDate (date:DateOnly) = this.TryFindByDate (date.ToString("yyyy-MM-dd"))
    member this.TryFindByDate (date:DateTimeOffset) = this.TryFindByDate (date.ToString("yyyy-MM-dd"))
