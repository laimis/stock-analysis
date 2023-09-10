module core.fs.Options.Chain

    open System
    open core.Account
    open core.Options
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.fs
    
    type Query(ticker:string, userId:Guid) =
        member this.Ticker = ticker
        member this.UserId = userId
        
    type Handler(accounts:IAccountStorage, brokerage:IBrokerage) =
        interface IApplicationService
        
        member this.Handle (query:Query) = task {
            let! user = accounts.GetUser(userId=query.UserId)
            
            match user with
            | null -> return ServiceResponse<OptionDetailsViewModel>(ServiceError("User not found"))
            | _ ->
                
                let! priceResult = brokerage.GetQuote(state=user.State, ticker=query.Ticker)
                let price = match priceResult.IsOk with
                            | true -> Nullable<decimal>(priceResult.Success.Price)
                            | false -> Nullable<decimal>()
                            
                let! details = brokerage.GetOptions(state=user.State, ticker=query.Ticker)
                
                match details.IsOk with
                | true -> 
                    let model = OptionDetailsViewModel(price, details.Success)
                    return ServiceResponse<OptionDetailsViewModel>(model)
                | false ->
                    return ServiceResponse<OptionDetailsViewModel>(ServiceError(details.Error.Message))
        }
            