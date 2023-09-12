module core.fs.Options.Details

    open System
    open core
    open core.Account
    open core.Options
    open core.Shared
    open core.Shared.Adapters.Brokerage
    open core.Shared.Adapters.Storage
    open core.fs

    type Query(optionId:Guid, userId:Guid) =
        member this.OptionId = optionId
        member this.UserId = userId
        
    type Handler(accounts:IAccountStorage, brokerage:IBrokerage, storage:IPortfolioStorage) =
        
        interface IApplicationService
        
        member this.Handle (query:Query) = task {
            let! user = accounts.GetUser(query.UserId)
            
            match user with
            | null ->
                return ServiceResponse<OwnedOptionView>(ServiceError("User not found"))
            | _ ->
                
                let! option = storage.GetOwnedOption(optionId=query.OptionId, userId=query.UserId)
                let! chain = brokerage.GetOptions(user.State, option.State.Ticker)
                let detail = chain.Success.FindMatchingOption(
                    strikePrice = option.State.StrikePrice,
                    expirationDate = option.State.ExpirationDate,
                    optionType = option.State.OptionType
                )
                
                return ServiceResponse<OwnedOptionView>(
                    OwnedOptionView(option.State, optionDetail = detail)
                );      
        }
            
            