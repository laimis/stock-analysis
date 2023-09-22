module core.fs.Options.BuyOrSell

    open System
    open core
    open core.Account
    open core.Options
    open core.Shared
    open core.Shared.Adapters.Storage
    open core.fs

    type Command =
        | Buy of OptionTransaction * Guid
        | Sell of OptionTransaction * Guid
    
    type Handler(accountStorage:IAccountStorage, storage:IPortfolioStorage) =
        
        let execute (cmd:Command) = task {
            
            let buy (opt:OwnedOption) (data:OptionTransaction) = opt.Buy(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)
            let sell (opt:OwnedOption) (data:OptionTransaction) = opt.Sell(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)
            
            let data, userId, func =
                match cmd with
                | Buy (buyData, userId) -> (buyData, userId, buy)
                | Sell (sellData, userId) -> (sellData, userId, sell)
            
            let! user = accountStorage.GetUser(userId)
            match user with
            | null -> return "User not found" |> ResponseUtils.failedTyped<OwnedOption>
            | _ ->
            
                let optionType = Enum.Parse(typedefof<OptionType>, data.OptionType) :?> OptionType
                
                let! options = storage.GetOwnedOptions(userId)
                let option =
                    options
                    |> Seq.tryFind (fun o -> o.IsMatch(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value))
                    |> Option.defaultWith (fun () -> OwnedOption(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value, userId))
                    
                func option data
                
                do! storage.Save(option, userId)
                
                return ServiceResponse<OwnedOption>(option);
        }
            
        interface IApplicationService
        
        member this.Handle (cmd:Command) = cmd |> execute
        