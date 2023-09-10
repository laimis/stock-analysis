module core.fs.Options.Expire

    open System
    open core
    open core.Options
    open core.Shared
    open core.fs
    
    type ExpireData(optionId: Guid, userId: Guid) =
        member this.OptionId = optionId
        member this.UserId = userId
        
    type Command =
        | Expire of ExpireData
        | Assign of ExpireData
        
    type ExpireViaLookupData(ticker:string, strikePrice:decimal, expiration:DateTimeOffset, userId: Guid) =
        member this.Ticker = ticker
        member this.StrikePrice = strikePrice
        member this.Expiration = expiration
        member this.UserId = userId
        
    type LookupCommand =
        | ExpireViaLookup of ExpireViaLookupData
        | AssignViaLookup of ExpireViaLookupData
                
    type Handler(storage: IPortfolioStorage) =
        
        let expireOption command = task {
            let (data,assign) = 
                match command with
                | Expire(data) -> (data, false)
                | Assign(data) -> (data, true)
                
            let! option = storage.GetOwnedOption(optionId = data.OptionId, userId = data.UserId)
            
            match option with
            | null ->
                return CommandResponse<OwnedOption>.Failed($"option for id {data.OptionId} not found")
            | _ ->
                option.Expire(assign=assign)
                let! _ = storage.Save(option, data.UserId)
                return CommandResponse<OwnedOption>.Success(option)
        }
        
        let expireViaLookup (command:LookupCommand) = task {
            let (data,assigned) = 
                match command with
                | ExpireViaLookup(data) -> (data, false)
                | AssignViaLookup(data) -> (data, true)
                
            let! options = storage.GetOwnedOptions(data.UserId)
            let option =
                options
                |> Seq.tryFind(fun o -> o.State.Ticker = data.Ticker && o.State.StrikePrice = data.StrikePrice && o.State.Expiration = data.Expiration)
                
            match option with
            | Some(o) ->
                o.Expire(assign=assigned)
                let! _ = storage.Save(o, data.UserId)
                return ServiceResponse<OwnedOption>(o)
                
            | None ->
                return ServiceResponse<OwnedOption>(ServiceError($"option for ticker {data.Ticker} strike {data.StrikePrice} expiration {data.Expiration} not found"))
        }
        
        interface IApplicationService
        
        member this.Handle(command: Command) = command |> expireOption
            
        member this.Handle(command: LookupCommand) = command |> expireViaLookup
            