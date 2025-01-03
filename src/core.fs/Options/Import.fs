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
        
        let processCommand record (command:Object) _ _ =
            match command with
            
            | :? ExpireViaLookupCommand as expCommand ->
                expCommand |> optionHandler.Handle |> Async.AwaitTask
                
            | :? BuyOrSellCommand as bsCommand ->
                bsCommand |> optionHandler.Handle |> Async.AwaitTask
                
            | _ -> Exception($"Handler for command type {record.GetType()} not available") |> raise
        
        let recordToCommand record userId =
            
            let toOptionTransaction record =
                {
                    ExpirationDate = record.expiration
                    Filled = Nullable<DateTimeOffset> record.filled
                    NumberOfContracts = record.amount
                    OptionType = OptionType.FromString record.optiontype
                    Premium = Nullable<decimal> record.premium
                    StrikePrice = Nullable<decimal> record.strike
                    Ticker = Ticker record.ticker
                    Notes = null 
                }
                
            let (command:Object) =
                
                let createExpireViaLookupData record : LookupData =
                    {
                        Ticker = Ticker record.ticker
                        StrikePrice = record.strike
                        Expiration = record.expiration.Value
                        UserId = userId
                    }
                
                match record.``type`` with
                | "sell" ->
                    BuyOrSellCommand.Sell(record |> toOptionTransaction, userId)
                    
                | "buy" ->
                    BuyOrSellCommand.Buy(record |> toOptionTransaction, userId)
                    
                | "expired" ->
                    ExpireViaLookupCommand.ExpireViaLookup(record |> createExpireViaLookupData)
                | "assigned" ->
                    ExpireViaLookupCommand.AssignViaLookup(record |> createExpireViaLookupData)
                | _ -> Exception($"Unexpected command type: {record.``type``}") |> raise
            
            (record,command)
        
        let runAsAsync token userId records =
                records
                |> Seq.map( fun record -> recordToCommand record userId)
                |> Seq.map(fun (record,command) -> processCommand record command userId token)
                |> Async.Sequential
                |> Async.StartAsTask

        interface IApplicationService

        member _.Handle (request:Command) token = task {
            let parseResponse = csvParser.Parse<OptionRecord>(request.Content)

            match parseResponse with
            | Error err -> return Error err
            | Ok response ->
                
                let! processedRecords = response |> runAsAsync token request.UserId
                
                let failed =
                    processedRecords
                    |> Array.map (fun x -> match x with | Ok _ -> None | Error err -> Some err)
                    |> Array.choose id
                    |> Array.tryHead
                
                return
                    match failed with
                    | None -> Ok ()
                    | Some err -> Error err
        }
            
