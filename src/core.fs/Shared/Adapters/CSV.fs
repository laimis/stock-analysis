namespace core.fs.Shared.Adapters.CSV

open core.Cryptos
open core.Notes
open core.Options
open core.Portfolio
open core.Shared
open core.Stocks
open core.fs.Shared.Domain.Accounts

type ExportResponse(filename:string, content:string) = 
    member this.Filename = filename
    member this.Content = content
    member this.ContentType = "text/csv"
        
type ICSVWriter =
    abstract Generate<'T> : rows:seq<'T> -> string
    
type ICSVParser =
    abstract Parse<'T> : content:string -> core.Shared.ServiceResponse<seq<'T>>
    