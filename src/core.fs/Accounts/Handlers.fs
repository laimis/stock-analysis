namespace core.fs.Accounts

    open System
    open System.ComponentModel.DataAnnotations
    open core.Account
    open core.Shared
    open core.fs.Shared
    open core.fs.Shared.Adapters.Authentication
    open core.fs.Shared.Adapters.Brokerage
    open core.fs.Shared.Adapters.Email
    open core.fs.Shared.Adapters.Storage
    open core.fs.Shared.Adapters.Subscriptions
    open core.fs.Shared.Domain.Accounts
    
    type AccountStatusView =
        {
            LoggedIn: bool
            Id: Guid
            Verified: bool
            Created: DateTimeOffset
            Email: string
            Firstname: string
            Lastname: string
            IsAdmin: bool
            SubscriptionLevel : string
            ConnectedToBrokerage: bool
            BrokerageAccessTokenExpired: bool
            MaxLoss: decimal option
        }

        static member fromUserState (isAdmin:bool) (state:UserState) =
            {
                LoggedIn = true
                Id = state.Id
                Verified = state.Verified.HasValue
                Created = state.Created
                Email = state.Email
                Firstname = state.Firstname
                Lastname = state.Lastname
                IsAdmin = isAdmin
                SubscriptionLevel = state.SubscriptionLevel
                ConnectedToBrokerage = state.ConnectedToBrokerage
                BrokerageAccessTokenExpired = state.BrokerageAccessTokenExpired
                MaxLoss = if state.MaxLoss.HasValue then Some state.MaxLoss.Value else None
            }
            
        static member notFound() =
            {
                LoggedIn = false
                Id = Guid.Empty
                Verified = false
                Created = DateTimeOffset.MinValue
                Email = null
                Firstname = null
                Lastname = null
                IsAdmin = false
                SubscriptionLevel = null
                ConnectedToBrokerage = false
                BrokerageAccessTokenExpired = true
                MaxLoss = None 
            }    
    
    module Authenticate =

        [<CLIMutable>]
        [<Struct>]
        type Command = {
            [<Required>]
            Email: string
            [<Required>]
            Password: string
        }
        
        type Handler(storage:IAccountStorage, hashProvider:IPasswordHashProvider, roles:IRoleService) =
            let INVALID_EMAIL_PASSWORD = "Invalid email/password combination"
        
            let attemptLogin (user:User) command = task {
                
                let validPassword =
                    hashProvider.GenerateHash command.Password (user.State.GetSalt())
                    |> user.PasswordHashMatches
                
                match validPassword with
                | false -> return INVALID_EMAIL_PASSWORD |> ResponseUtils.failedTyped<AccountStatusView>
                | true -> return ServiceResponse<AccountStatusView> (user.State |> AccountStatusView.fromUserState (roles.IsAdmin user.State))
            }
            
            interface IApplicationService
            member this.Handle (command:Command) = task {
                let! user = storage.GetUserByEmail(command.Email)
                
                match user with
                | None -> return INVALID_EMAIL_PASSWORD |> ResponseUtils.failedTyped<AccountStatusView>
                | Some user -> return! command |> attemptLogin user
            }
            
    module ClearAccount =
        
        [<Struct>]
        type Command = {
            UserId: UserId
        }
        
        type Handler(storage:IAccountStorage, portfolioStorage:IPortfolioStorage) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! user = storage.GetUser(command.UserId)
                
                match user with
                | None -> return "User not found" |> ResponseUtils.failed
                | Some user ->
                    do! portfolioStorage.Delete(user.Id |> UserId)
                    return Ok
            }
            
    module DeleteAccount =
        
        type Command =
            {
                UserId: UserId
                Feedback: string
            }
        
            member this.WithUserId userId = { this with UserId = userId }
       
        
        type Handler(storage:IAccountStorage, portfolio:IPortfolioStorage, email:IEmailService, roleService:IRoleService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! user = storage.GetUser(command.UserId)
                
                match user with
                | None -> return "User not found" |> ResponseUtils.failed
                | Some user ->
                    user.Delete(command.Feedback)
                    do! storage.Save(user)
                    
                    // TODO: technically it would be better to mark the user as deleted and then have a background process
                    // to send emails, delete the actual user record, and delete portfolio data
                    let properties = {|feedback = command.Feedback; email = user.State.Email;|}
                    do! email.SendWithTemplate (Recipient(email=roleService.GetAdminEmail(), name="Admin")) Sender.NoReply EmailTemplate.AdminUserDeleted properties
                        
                    do! storage.Delete(user)
                    do! portfolio.Delete(user.Id |> UserId)
                    return Ok
            }
            
    module ConfirmAccount =
        
        [<Struct>]
        type Command = {
            Id: Guid
        }
        
        type Handler(storage:IAccountStorage, email:IEmailService, roleService:IRoleService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! association = storage.GetUserAssociation(command.Id)
                match association with
                | None -> return "Invalid confirmation identifier." |> ResponseUtils.failedTyped<AccountStatusView>
                | Some association ->
                    let! user = storage.GetUser(association.UserId)
                    match user with
                    | None -> return "User not found" |> ResponseUtils.failedTyped<AccountStatusView>
                    | Some user ->
                        
                        match association.IsOlderThan(TimeSpan.FromDays(30)) with
                        | true -> return "Confirmation link is expired. Please request a new one." |> ResponseUtils.failedTyped<AccountStatusView>
                        | false ->
                            user.Confirm()
                            do! storage.Save(user)
                            do! email.SendWithTemplate (Recipient(email = user.State.Email, name = user.State.Name)) Sender.Support EmailTemplate.NewUserWelcome {||}
                            return user.State |> AccountStatusView.fromUserState (roleService.IsAdmin(user.State)) |> ResponseUtils.success<AccountStatusView>
            }
            
    module Contact =
        
        [<CLIMutable>]
        [<Struct>]
        type Command = {
            [<Required>]
            Email: string
            [<Required>]
            Message: string
        }
        
        type Handler(emailService:IEmailService, roleService:IRoleService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                do! emailService.SendWithTemplate (Recipient(email=roleService.GetAdminEmail(), name=null)) Sender.NoReply EmailTemplate.AdminContact {|message = command.Message; email = command.Email|}
                return Ok
            }
            
    module Create =
        
        [<CLIMutable>]
        [<Struct>]
        type PaymentToken = {
            [<Required>]
            Id: string
            [<Required>]
            Email: string
        }
        
        [<CLIMutable>]
        [<Struct>]
        type PaymentInfo = {
            [<Required>]
            Token: PaymentToken
            [<Required>]
            PlanId: string
        }
        
        type RequiredPasswordAttribute() = 
            inherit ValidationAttribute()
            
            override this.IsValid (value:obj) =
                match value with
                | null -> false
                | _ ->
                    let password = value :?> string
                    password.Length >= 10 && password.Length <= 1000
        
        [<CLIMutable>]
        [<Struct>]
        type UserInfo = {
            [<Required>]
            Email: string
            [<RequiredPassword>]
            Password: string
            [<Required>]
            FirstName: string
            [<Required>]
            LastName: string
            [<Required>]
            [<Range(typedefof<bool>, "true", "true", ErrorMessage="You must accept the terms and conditions.")>]
            Terms: bool
        }
        
        [<CLIMutable>]
        [<Struct>]
        type Command = {
            [<Required>]
            UserInfo: UserInfo
            [<Required>]
            PaymentInfo: PaymentInfo
        }
        
        [<CLIMutable>]
        [<Struct>]
        type ResetPassword = {
            Id:Guid
            [<RequiredPassword>]
            Password:string
        }
        
        type SendCreateNotifications =
            {
                UserId: UserId
                Email: string
                FirstName: string
                LastName: string
                Created: DateTimeOffset
            }
        
        type Handler(
            storage:IAccountStorage,
            hashProvider:IPasswordHashProvider,
            email:IEmailService,
            roleService:IRoleService,
            subscriptions:ISubscriptions) =
            
            let processPaymentInfo user paymentInfo =
                let result = subscriptions.Create user paymentInfo.Token.Email paymentInfo.PlanId paymentInfo.Token.Id
                
                match result.CustomerId with
                | null ->
                    $"Failed to process the payment, please try again or use a different payment form" |> ResponseUtils.failed
                | _ ->
                    user.SubscribeToPlan paymentInfo.PlanId result.CustomerId result.SubscriptionId
                    Ok
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                
                let! exists = storage.GetUserByEmail(command.UserInfo.Email)
                match exists with
                | Some _ -> return $"Account with {command.UserInfo.Email} already exists" |> ResponseUtils.failedTyped<AccountStatusView>
                | None ->
                    let u = User.Create(email=command.UserInfo.Email, firstname=command.UserInfo.FirstName, lastname=command.UserInfo.LastName)
                    
                    let hashAndSalt = hashProvider.GenerateHashAndSalt command.UserInfo.Password 32
                    
                    u.SetPassword hashAndSalt.Hash hashAndSalt.Salt
                    
                    let paymentResponse = command.PaymentInfo |> processPaymentInfo u
                    
                    match paymentResponse with
                    | Error err ->
                        return err.Message |> ResponseUtils.failedTyped<AccountStatusView>
                    | _ ->
                        do! storage.Save(u)
                        return ServiceResponse<AccountStatusView>(u.State |> AccountStatusView.fromUserState (roleService.IsAdmin u.State))
            }
            
            member this.Validate (userInfo:UserInfo) = task {
                let! exists = storage.GetUserByEmail(userInfo.Email)
                match exists with
                | None -> return Ok
                | Some _ -> return $"Account with {userInfo.Email} already exists" |> ResponseUtils.failed
            }
            
            member this.Handle (reset:ResetPassword) = task {
                let! association = storage.GetUserAssociation(reset.Id)
                match association with
                | None -> return "Invalid password reset token. Check the link in the email or request a new password reset." |> ResponseUtils.failedTyped<AccountStatusView>
                | Some association ->
                    let! user = association.UserId |>  storage.GetUser
                    match user with
                    | None -> return "User account is no longer valid" |> ResponseUtils.failedTyped<AccountStatusView>
                    | Some user ->
                        match association.IsOlderThan(TimeSpan.FromMinutes(15)) with
                        | true -> return "Password reset link is expired. Please request a new one." |> ResponseUtils.failedTyped<AccountStatusView>
                        | false ->
                            let hashAndSalt = hashProvider.GenerateHashAndSalt reset.Password 32
                            user.SetPassword hashAndSalt.Hash hashAndSalt.Salt
                            do! storage.Save(user)
                            return user.State |> AccountStatusView.fromUserState (roleService.IsAdmin(user.State)) |> ResponseUtils.success<AccountStatusView>
            }
            
            member this.Handle (createNotifications:SendCreateNotifications) = task {
                
                let! user = storage.GetUser(createNotifications.UserId)
                match user with
                | None -> return "User not found" |> ResponseUtils.failed
                | Some user ->
                    let request = ProcessIdToUserAssociation(createNotifications.UserId, createNotifications.Created)
                    
                    do! storage.SaveUserAssociation(request)
                    
                    let confirmUrl = $"{EmailSettings.ConfirmAccountUrl}/{request.Id}"
                    
                    do! email.SendWithTemplate (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply EmailTemplate.ConfirmAccount {|confirmurl = confirmUrl|}
                    do! email.SendWithTemplate (Recipient(email=roleService.GetAdminEmail(), name="Admin")) Sender.NoReply EmailTemplate.AdminNewUser {|email = user.State.Email; |}
                    
                    return Ok
            }
            
    
    
    module PasswordReset =
        
        [<CLIMutable>]
        [<Struct>]
        type RequestCommand = {
            [<Required>]
            Email: string
        }
        
        type Handler(storage:IAccountStorage,emailService:IEmailService) =
            
            interface IApplicationService
            
            member this.Handle request = task {
                let! user = storage.GetUserByEmail(request.Email)
                
                match user with
                | None -> return Ok // don't return an error so that user accounts can't be enumerated
                | Some user ->
                    let association = ProcessIdToUserAssociation(user.Id |> UserId, DateTimeOffset.UtcNow)
                    
                    do! storage.SaveUserAssociation(association)
                    
                    let resetUrl = $"{EmailSettings.PasswordResetUrl}/{association.Id}"
                    
                    do! emailService.SendWithTemplate (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply EmailTemplate.PasswordReset {|reseturl = resetUrl|}
                    
                    return Ok
            }
            
    module Status =
        
        type LookupByEmail = {
            Email:string
        }
        
        type LookupById = {
            UserId:UserId
        }
        
        type Handler(storage:IAccountStorage, roleService:IRoleService) =
            
            let successOrError (user:User option) =
                match user with
                    | None -> AccountStatusView.notFound() 
                    | Some user -> user.State |> AccountStatusView.fromUserState (roleService.IsAdmin(user.State))
                |> ResponseUtils.success<AccountStatusView>
                
            interface IApplicationService
            
            member this.Handle (query:LookupById) = task {
                let! user = storage.GetUser(query.UserId)
                return user |> successOrError
            }
            
            member this.Handle (query:LookupByEmail) = task {
                let! user = storage.GetUserByEmail(query.Email)
                
                return user |> successOrError
            }
            
    module Brokerage =
        type Connect = struct end
    
        [<Struct>]
        type ConnectCallback = {
            Code:string
            UserId:UserId
        }
        
        [<Struct>]
        type Disconnect =
            {
                UserId:UserId
            }
            
        [<Struct>]
        type Info =
            {
                UserId:UserId
            }
    
        type Handler(accounts:IAccountStorage, brokerage:IBrokerage) = 
            
            interface IApplicationService
            
            member this.HandleConnect(_:Connect) = 
                brokerage.GetOAuthUrl()
                
            member this.HandleConnectCallback(cmd:ConnectCallback) = task {
                let! r = brokerage.ConnectCallback(cmd.Code)
                
                let! user = accounts.GetUser(cmd.UserId)
                match user with
                None -> return "User not found" |> ResponseUtils.failed
                | Some user ->
                    user.ConnectToBrokerage r.access_token r.refresh_token r.token_type r.expires_in r.scope r.refresh_token_expires_in
                    do! accounts.Save(user)
                    return Ok
            }
            
            member this.HandleDisconnect(cmd:Disconnect) = task {
                let! user = accounts.GetUser(cmd.UserId)
                match user with
                | None -> return "User not found" |> ResponseUtils.failed
                | Some user ->
                    user.DisconnectFromBrokerage()
                    do! accounts.Save(user)
                    return Ok
            }
            
            member this.HandleInfo(cmd:Info) = task {
                let! user = accounts.GetUser(cmd.UserId)
                match user with
                | None -> return "User not found" |> ResponseUtils.failedTyped<OAuthResponse>
                | Some user ->
                    let! oauth = brokerage.GetAccessToken(user.State)
                    return ServiceResponse<OAuthResponse>(oauth)
            }
            
    module Settings =
        
        [<CLIMutable>]
        type Command =
            {
                UserId: UserId
                [<Required>]
                Key: string
                [<Required>]
                Value: string
            }
        
            member this.WithUserId userId = { this with UserId = userId }
        
        type Handler(storage:IAccountStorage, roleService:IRoleService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! user = storage.GetUser(command.UserId)
                
                match user with
                | None -> return "User not found" |> ResponseUtils.failedTyped<AccountStatusView>
                | Some user ->
                    user.SetSetting command.Key command.Value
                    do! storage.Save(user)
                    return ServiceResponse<AccountStatusView>(user.State |> AccountStatusView.fromUserState (user.State |> roleService.IsAdmin))
            }