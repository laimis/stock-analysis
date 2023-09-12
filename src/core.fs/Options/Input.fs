namespace core.fs.Options

open System
open System.ComponentModel.DataAnnotations
open core.Utils


type OptionTransaction() =
    
    [<Range(1, 10000)>]
    [<Required>]
    member val StrikePrice : Nullable<decimal> = Nullable<decimal>() with get, set
        
    [<Required>]
    member val ExpirationDate : Nullable<DateTimeOffset> = Nullable<DateTimeOffset>() with get, set
        
    [<Required>]
    [<ValidValues("CALL", "PUT")>]
    member val OptionType : string = null with get, set
        
    [<Range(1, 10000, ErrorMessage = "Invalid number of contracts specified")>]
    member val NumberOfContracts : int = 1 with get, set
        
    [<Range(1, 100000)>]
    [<Required>]
    member val Premium : Nullable<decimal> = Nullable<decimal>() with get, set
        
    [<Required>]
    member val Filled : Nullable<DateTimeOffset> = Nullable<DateTimeOffset>() with get, set
    
    member val Notes : string = "" with get, set
    
    member val UserId : Guid = Guid.Empty with get, set
    
    [<Required>]
    member val Ticker : string = "" with get, set
