module core.fs.Options.BuyOrSell

    open System
    open core
    open core.Account
    open core.Options
    open core.Shared
    open core.fs

    type Command =
        | Buy of OptionTransaction
        | Sell of OptionTransaction
    
    type Handler(accountStorage:IAccountStorage, storage:IPortfolioStorage) =
        
        let execute (cmd:Command) = task {
            
            let buy (opt:OwnedOption) (data:OptionTransaction) = opt.Buy(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)
            let sell (opt:OwnedOption) (data:OptionTransaction) = opt.Sell(data.NumberOfContracts, data.Premium.Value, data.Filled.Value, data.Notes)
            
            let (data, func) =
                match cmd with
                | Buy buyData -> (buyData, buy)
                | Sell sellData -> (sellData, sell)
                
            let optionType = System.Enum.Parse(typedefof<OptionType>, data.OptionType) :?> OptionType
            
            let! options = storage.GetOwnedOptions(data.UserId)
            let option =
                options
                |> Seq.tryFind (fun o -> o.IsMatch(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value))
                |> Option.defaultWith (fun () -> OwnedOption(data.Ticker, data.StrikePrice.Value, optionType, data.ExpirationDate.Value, data.UserId))
                
            func option data
            
            let! _ = storage.Save(option, data.UserId)
            
            return ServiceResponse<OwnedOption>(option);
        }
            
        interface IApplicationService
        
        member this.Handle (cmd:Command) = task {
            
            let data =
                match cmd with
                | Buy data -> data
                | Sell data -> data
                
            let! user = accountStorage.GetUser(data.UserId)
            
            match user with
            | null ->
                return ServiceResponse<OwnedOption>(
                    ServiceError(
                        "Unable to find user account for options operation"
                    )
                )
            | _ ->
                return! cmd |> execute
        }
        