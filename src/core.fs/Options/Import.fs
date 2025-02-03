namespace core.fs.Options

open System
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.CSV

module Import =
    open core.Shared

    type OptionRecord = {
        ticker:string
        strike:decimal
        optiontype:string
        expiration:Nullable<DateTimeOffset>
        amount:int
        premium:decimal
        filled:DateTimeOffset
        ``type``:string
    }
          
    type Command(content:string,userId:UserId) =
        member _.UserId = userId
        member _.Content = content

    type Handler(csvParser:ICSVParser, optionHandler:core.fs.Options.OptionsHandler) =
        
        member _.Handle (request:Command) token = task {
            let parseResponse = csvParser.Parse<OptionRecord>(request.Content)

            match parseResponse with
            | Error err -> return Error err
            | Ok _ -> return "Not implemented" |> ServiceError |> Error
        }
            
