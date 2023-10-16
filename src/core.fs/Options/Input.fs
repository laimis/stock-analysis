namespace core.fs.Options

open System
open System.ComponentModel.DataAnnotations
open core.Shared

type OptionType =
    | Call
    | Put
    
    with
        override this.ToString() =
            match this with
            | Call -> nameof Call
            | Put -> nameof Put
            
        member this.ToEnum() =
            match this with
            | Call -> core.Options.OptionType.CALL
            | Put -> core.Options.OptionType.PUT
            
        static member FromString(value:string) =
            match value with
            | nameof Call -> Call
            | nameof Put -> Put
            | _ -> failwithf $"Invalid option type: %s{value}"
            
type OptionTransaction =
    {
        [<Range(1, 10000)>]
        [<Required>]
        StrikePrice:Nullable<decimal>
        [<Required>]
        ExpirationDate : Nullable<DateTimeOffset>
        [<Required>]
        OptionType : OptionType        
        [<Range(1, 10000, ErrorMessage = "Invalid number of contracts specified")>]
        NumberOfContracts : int
        [<Range(1, 100000)>]
        [<Required>]
        Premium : Nullable<decimal>
        [<Required>]
        Filled : Nullable<DateTimeOffset>
        Notes : string
        [<Required>]
        Ticker : Ticker
    }
    