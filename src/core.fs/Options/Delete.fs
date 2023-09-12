module core.fs.Options.Delete

    open System
    open core
    open core.Shared
    open core.Shared.Adapters.Storage
    open core.fs

    type Command(optionId:Guid, userId:Guid) =
        member this.OptionId = optionId
        member this.UserId = userId
        
    type Handler(storage:IPortfolioStorage) =
        
        interface IApplicationService
        member this.Handle(command:Command) = task {
            let! opt = storage.GetOwnedOption(optionId=command.OptionId, userId=command.UserId)
            
            match opt with
            | null ->
                return ServiceResponse(ServiceError("Unable to find option do delete"))
            | _ ->
                opt.Delete()
                let! _ = storage.Save(opt, command.UserId)
                return ServiceResponse()
        }