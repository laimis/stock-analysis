namespace core.fs.Options

open System
open core.Shared.Adapters.CSV
open core.fs.Shared

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
          
    type Command(content:string,userId:Guid) =
        member _.UserId = userId
        member _.Content = content

    type Handler(csvParser:ICSVParser, optionHandler:core.fs.Options.Handler) =
        
        let processCommand record (command:Object) userId token =
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
                    OptionType = record.optiontype
                    Premium = Nullable<decimal> record.premium
                    StrikePrice = Nullable<decimal> record.strike
                    Ticker = Ticker record.ticker
                    Notes = null 
                }
                
            let (command:Object) =
                match record.``type`` with
                | "sell" ->
                    BuyOrSellCommand.Sell(record |> toOptionTransaction, userId)
                    
                | "buy" ->
                    BuyOrSellCommand.Buy(record |> toOptionTransaction, userId)
                    
                | "expired" ->
                    ExpireViaLookupCommand.ExpireViaLookup(
                        ExpireViaLookupData(ticker=record.ticker,strikePrice=record.strike,expiration=record.expiration.Value,userId=userId)
                    )
                | "assigned" ->
                    ExpireViaLookupCommand.AssignViaLookup(
                        ExpireViaLookupData(ticker=record.ticker,strikePrice=record.strike,expiration=record.expiration.Value,userId=userId)
                    )
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

            match parseResponse.IsOk with
            | false ->
                return parseResponse.Error.Message |> ResponseUtils.failed
            | true ->
                
                let! processedRecords = parseResponse.Success |> runAsAsync token request.UserId  
                let failed = processedRecords |> Seq.filter (fun r -> r.Error = null |> not) |> Seq.tryHead
                
                let finalResult =
                    match failed with
                    | None -> ServiceResponse()
                    | Some f -> f.Error.Message |> ResponseUtils.failed
                    
                return finalResult
        }
            