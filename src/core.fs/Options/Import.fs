namespace core.fs.Options

open System
open core.Options

module Import =
    open core.fs
    open core
    open core.Adapters.CSV
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

    type Handler(csvParser:ICSVParser, mediator:MediatR.IMediator, expireHandler:Expire.Handler) =
        
        let handleOptionTransaction (record:OptionRecord) (opt:OptionTransaction) userId token =
            opt.ExpirationDate <- record.expiration;
            opt.Filled <- record.filled;
            opt.NumberOfContracts <- record.amount;
            opt.OptionType <- record.optiontype;
            opt.Premium <- record.premium;
            opt.StrikePrice <- record.strike;
            opt.Ticker <- record.ticker;
            opt.WithUserId(userId);

            mediator.Send(opt, token) |> Async.AwaitTask
        
        let handleExpireCommand (ec:Expire.LookupCommand) = expireHandler.Handle ec |> Async.AwaitTask
        
        let processCommand record (command:Object) userId token =
            match command with
            | :? OptionTransaction as opt ->
                Console.WriteLine($"processing {opt.GetType()} for {opt.Ticker} {opt.Premium} {opt.ExpirationDate}")
                handleOptionTransaction record opt userId token
                
            | :? Expire.LookupCommand as expCommand ->
                Console.WriteLine($"processing {expCommand.GetType()} for {record.ticker} {record.premium} {record.expiration}")
                handleExpireCommand expCommand
                
            | _ -> Exception($"Handler for command type {record.GetType()} not available") |> raise
        
        let recordToCommand record userId =
            let (command:Object) =
                match record.``type`` with
                | "sell" -> Sell.Command()
                | "buy" -> Buy.Command()
                | "expired" ->
                    Expire.ExpireViaLookup(
                        Expire.ExpireViaLookupData(ticker=record.ticker,strikePrice=record.strike,expiration=record.expiration.Value,userId=userId)
                    )
                | "assigned" ->
                    Expire.AssignViaLookup(
                        Expire.ExpireViaLookupData(ticker=record.ticker,strikePrice=record.strike,expiration=record.expiration.Value,userId=userId)
                    )
                | _ -> Exception($"Unexpected command type: {record.``type``}") |> raise
            Console.WriteLine($"Converted {record.``type``} to {command.GetType()}")
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
                return CommandResponse.Failed(parseResponse.Error.Message)
            | true ->
                
                let! processedRecords = parseResponse.Success |> runAsAsync token request.UserId  
                let failed = processedRecords |> Seq.filter (fun r -> r.Error = null |> not) |> Seq.tryHead
                
                let finalResult =
                    match failed with
                    | None -> CommandResponse.Success()
                    | Some f -> CommandResponse.Failed(f.Error)
                    
                return finalResult
        }
            