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

    type Handler(storage:IPortfolioStorage, csvParser:ICSVParser, mediator:MediatR.IMediator) =
        
        let handleOptionTransaction (record:OptionRecord) (opt:OptionTransaction) userId token = async {
            opt.ExpirationDate <- record.expiration;
            opt.Filled <- record.filled;
            opt.NumberOfContracts <- record.amount;
            opt.OptionType <- record.optiontype;
            opt.Premium <- record.premium;
            opt.StrikePrice <- record.strike;
            opt.Ticker <- record.ticker;
            opt.WithUserId(userId);

            let! result = mediator.Send(opt, token) |> Async.AwaitTask
            Console.WriteLine($"sent {opt.Ticker} to {opt.GetType()} and received {result.Aggregate.State.Ticker}");
            return result
        }
        
        let handleExpireCommand record (ec:Options.Expire.Command) userId token = async {
            let! opts = storage.GetOwnedOptions(userId) |> Async.AwaitTask

            let optType = Enum.Parse(typedefof<OptionType>, record.optiontype) :?> OptionType

            let opt =
                opts
                |> Seq.filter (fun o -> o.IsMatch(record.ticker, record.strike, optType, record.expiration.Value))
                |> Seq.tryHead

            match opt with
            | None ->
                Console.WriteLine("unable to find option to expire")
                return CommandResponse.Failed($"Unable to find option to expire for {record.ticker} {record.strike} {record.optiontype} {record.expiration}")
            | Some optValue ->

                Console.WriteLine($"sent {optValue.State.Ticker} to expire");
                ec.Id <- optValue.Id;
                ec.WithUserId(userId);

                let! response = mediator.Send(ec, token) |> Async.AwaitTask
                Console.WriteLine($"Received: " + response.Aggregate.State.Ticker)
                return response
        }
        
        let recordTypeToCommand recordType : RequestWithUserIdBase =
            match recordType with
                | "sell" -> Sell.Command()
                | "buy" -> Buy.Command()
                | "expired" -> Expire.UnassignedCommand()
                | "assigned" -> Expire.AssignedCommand()
                | _ -> Exception($"Unexpected command type: {recordType}") |> raise
        
        let processCommand record (command:Object) userId token = async {

            
            let! commandResponse =
                match command with
                | :? OptionTransaction as opt ->
                    System.Console.WriteLine($"processing {opt.GetType()} for {opt.Ticker} {opt.Premium} {opt.ExpirationDate}")
                    handleOptionTransaction record opt userId token
                    
                | :? core.Options.Expire.Command as expCommand ->
                    System.Console.WriteLine($"processing {expCommand.GetType()} for {record.ticker} {record.premium} {record.expiration}")
                    handleExpireCommand record expCommand userId token
                    
                | _ -> Exception($"Handler for command type {record.GetType()} not available") |> raise
                    
            Console.WriteLine($"Response with error? " + commandResponse.Error)    
            return commandResponse
        }
        
        let runAsAsync token userId records = 
            records
            |> Seq.map( fun record ->
                    let command = record.``type`` |> recordTypeToCommand
                    System.Console.WriteLine($"Converted {record.``type``} to {command.GetType()}")
                    (record,command)
            )
            |> Seq.map(fun (record,command) -> processCommand record command userId token)
            |> Async.Sequential
            |> Async.RunSynchronously

        interface IApplicationService

        member _.Handle (request:Command) token =
            let parseResponse = csvParser.Parse<OptionRecord>(request.Content)

            match parseResponse.IsOk with
            | false ->
                CommandResponse.Failed(parseResponse.Error.Message)
            | true ->
                
                let processedRecords = parseResponse.Success |> runAsAsync token request.UserId  
                let failed = processedRecords |> Seq.filter (fun r -> r.Error = null |> not) |> Seq.tryHead
                
                match failed with
                | None -> CommandResponse.Success()
                | Some f -> CommandResponse.Failed(f.Error)