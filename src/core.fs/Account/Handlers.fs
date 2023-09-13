namespace core.fs.Account

    open System
    open System.ComponentModel.DataAnnotations
    open core.Account
    open core.Shared
    open core.Shared.Adapters.Storage
    open core.fs
    
    module Authenticate =

        [<CLIMutable>]
        [<Struct>]
        type Command = {
            [<Required>]
            Email: string
            [<Required>]
            Password: string
        }
        
        type Handler(storage:IAccountStorage, hashProvider:IPasswordHashProvider) =
            let INVALID_EMAIL_PASSWORD = "Invalid email/password combination"
        
            let attemptLogin (user:User) command = task {
                
                let validPassword =
                    hashProvider.Generate(password=command.Password, salt=user.State.GetSalt())
                    |> user.PasswordHashMatches
                
                match validPassword with
                | false -> return INVALID_EMAIL_PASSWORD |> ResponseUtils.failedTyped<User>
                | true ->
                    user.LoggedIn(null, DateTimeOffset.UtcNow)
                    let! _ = storage.Save(user)
                    return ServiceResponse<User>(user)
            }
            
            interface IApplicationService
            member this.Handle (command:Command) = task {
                let! user = storage.GetUserByEmail(command.Email)
                
                match user with
                | null -> return INVALID_EMAIL_PASSWORD |> ResponseUtils.failedTyped<User>
                | _ -> return! command |> attemptLogin user
            }
            
    module ClearAccount =
        
        [<Struct>]
        type Command = {
            UserId: Guid
        }
        
        type Handler(storage:IAccountStorage, portfolioStorage:IPortfolioStorage) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! user = storage.GetUser(command.UserId)
                
                match user with
                | null -> return "User not found" |> ResponseUtils.failed
                | _ ->
                    let! _ = portfolioStorage.Delete(user.Id)
                    return ServiceResponse()
            }