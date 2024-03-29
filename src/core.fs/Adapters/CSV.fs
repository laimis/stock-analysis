namespace core.fs.Adapters.CSV

open core.fs

type ExportResponse(filename:string, content:string) = 
    member this.Filename = filename
    member this.Content = content
    member this.ContentType = "text/csv"
        
type ICSVWriter =
    abstract Generate<'T> : rows:seq<'T> -> string
    
type ICSVParser =
    abstract Parse<'T> : content:string -> Result<seq<'T>,ServiceError>
    
