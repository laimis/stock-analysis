namespace core.fs.Accounts

    open System
    open System.ComponentModel.DataAnnotations
    open core.Account
    open core.Shared
    open core.Shared.Adapters
    open core.Shared.Adapters.Brokerage
    open core.Shared.Adapters.Emails
    open core.Shared.Adapters.Storage
    open core.Shared.Adapters.Subscriptions
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
                | true -> return ServiceResponse<User>(user)
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
                    do! portfolioStorage.Delete(user.Id)
                    return ServiceResponse()
            }
            
    module DeleteAccount =
        
        type Command =
            {
                UserId: Guid
                Feedback: string
            }
        
            member this.WithUserId (userId:Guid) = { this with UserId = userId }
       
        
        type Handler(storage:IAccountStorage, portfolio:IPortfolioStorage, email:IEmailService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! user = storage.GetUser(command.UserId)
                
                match user with
                | null -> return "User not found" |> ResponseUtils.failed
                | _ ->
                    user.Delete(command.Feedback)
                    do! storage.Save(user)
                    
                    // TODO: technically it would be better to mark the user as deleted and then have a background process
                    // to send emails, delete the actual user record, and delete portfolio data
                    do! email.Send(
                        recipient= EmailSettings.Admin,
                        sender=Sender.NoReply,
                        template=EmailTemplate.AdminUserDeleted,
                        properties= {|feedback = command.Feedback; email = user.State.Email;|}
                    )
                    do! storage.Delete(user)
                    do! portfolio.Delete(user.Id)
                    return ServiceResponse()
            }
            
    module ConfirmAccount =
        
        [<Struct>]
        type Command = {
            Id: Guid
        }
        
        type Handler(storage:IAccountStorage, email:IEmailService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                let! association = storage.GetUserAssociation(command.Id)
                match association with
                | null -> return "Invalid confirmation identifier." |> ResponseUtils.failedTyped<User>
                | _ ->
                    let! user = storage.GetUser(association.UserId)
                    match user with
                    | null -> return "User not found" |> ResponseUtils.failedTyped<User>
                    | _ ->
                        
                        match association.IsOlderThan(TimeSpan.FromDays(30)) with
                        | true -> return "Confirmation link is expired. Please request a new one." |> ResponseUtils.failedTyped<User>
                        | false ->
                            user.Confirm()
                            do! storage.Save(user)
                            
                            do! email.Send(
                                Recipient(email = user.State.Email, name = user.State.Name),
                                Sender.Support,
                                EmailTemplate.NewUserWelcome,
                                {||}
                            );
                            return user |> ResponseUtils.success<User>
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
        
        type Handler(emailService:IEmailService) =
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                do! emailService.Send(
                    recipient = EmailSettings.Admin,
                    sender = Sender.NoReply,
                    template = EmailTemplate.AdminContact,
                    properties = {|message = command.Message; email = command.Email|}
                )
                return ServiceResponse()
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
                UserId: Guid
                Email: string
                FirstName: string
                LastName: string
                Created: DateTimeOffset
            }
        
        type Handler(
            storage:IAccountStorage,
            hashProvider:IPasswordHashProvider,
            email:IEmailService,
            subscriptions:ISubscriptions) =
            
            let processPaymentInfo user paymentInfo =
                let result = subscriptions.Create(
                    user=user,
                    email=paymentInfo.Token.Email,
                    planId=paymentInfo.PlanId,
                    paymentToken=paymentInfo.Token.Id
                )
                
                match result.CustomerId with
                | null ->
                    $"Failed to process the payment, please try again or use a different payment form" |> ResponseUtils.failed
                | _ ->
                    user.SubscribeToPlan(paymentInfo.PlanId, result.CustomerId, result.SubscriptionId);
                    ServiceResponse()
            
            interface IApplicationService
            
            member this.Handle (command:Command) = task {
                
                let! exists = storage.GetUserByEmail(command.UserInfo.Email)
                match exists with
                | null -> return $"Account with {command.UserInfo.Email} already exists" |> ResponseUtils.failedTyped<User>
                | _ ->
                    let u = User(email=command.UserInfo.Email, firstname=command.UserInfo.FirstName, lastname=command.UserInfo.LastName)
                    
                    let struct(hash, salt) = hashProvider.Generate(command.UserInfo.Password, saltLength=32);
                    
                    u.SetPassword(hash=hash, salt=salt)
                    
                    let paymentResponse = command.PaymentInfo |> processPaymentInfo u
                    
                    match paymentResponse.IsOk with
                    | false ->
                        return paymentResponse.Error.Message |> ResponseUtils.failedTyped<User>
                    | _ ->
                        do! storage.Save(u)
                        return ServiceResponse<User>(u)
            }
            
            member this.Validate (userInfo:UserInfo) = task {
                let! exists = storage.GetUserByEmail(userInfo.Email)
                match exists with
                | null -> return ServiceResponse()
                | _ -> return $"Account with {userInfo.Email} already exists" |> ResponseUtils.failed
            }
            
            member this.Handle (reset:ResetPassword) = task {
                let! association = storage.GetUserAssociation(reset.Id)
                match association with
                | null -> return "Invalid password reset token. Check the link in the email or request a new password reset." |> ResponseUtils.failedTyped<User>
                | _ ->
                    let! user = storage.GetUser(association.UserId)
                    match user with
                    | null -> return "User account is no longer valid" |> ResponseUtils.failedTyped<User>
                    | _ ->
                        match association.IsOlderThan(TimeSpan.FromMinutes(15)) with
                        | true -> return "Password reset link is expired. Please request a new one." |> ResponseUtils.failedTyped<User>
                        | false ->
                            let struct(hash, salt) = hashProvider.Generate(reset.Password, saltLength=32);
                            user.SetPassword(hash=hash, salt=salt)
                            do! storage.Save(user)
                            return user |> ResponseUtils.success<User>
            }
            
            member this.Handle (createNotifications:SendCreateNotifications) = task {
                
                let! user = storage.GetUser(createNotifications.UserId)
                match user with
                | null -> return "User not found" |> ResponseUtils.failed
                | _ ->
                    let request = ProcessIdToUserAssociation(createNotifications.UserId, createNotifications.Created)
                    
                    do! storage.SaveUserAssociation(request)
                    
                    let confirmUrl = $"{EmailSettings.ConfirmAccountUrl}/{request.Id}"
                    
                    do! email.Send(
                        recipient=Recipient(email=user.State.Email, name=user.State.Name),
                        sender=Sender.NoReply,
                        template=EmailTemplate.ConfirmAccount,
                        properties= {|confirmurl = confirmUrl|}
                    )
                    
                    do! email.Send(
                        recipient=EmailSettings.Admin,
                        sender=Sender.NoReply,
                        template=EmailTemplate.AdminNewUser,
                        properties= {|email = user.State.Email; |}
                    )
                    
                    return ServiceResponse()
            }
            
    
    open Create
    
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
                | null -> return ServiceResponse() // don't return an error so that user accounts can't be enumerated
                | _ ->
                    let association = new ProcessIdToUserAssociation(user.Id, DateTimeOffset.UtcNow)
                    
                    do! storage.SaveUserAssociation(association)
                    
                    let resetUrl = $"{EmailSettings.PasswordResetUrl}/{association.Id}"
                    
                    do! emailService.Send(
                        recipient=Recipient(email=user.State.Email, name=user.State.Name),
                        sender=Sender.NoReply,
                        template=EmailTemplate.PasswordReset,
                        properties= {|reseturl = resetUrl|}
                    )
                    return ServiceResponse()
            }
            
    module Status =
        
        type Handler(storage:IAccountStorage) =
            
            interface IApplicationService
            
            member this.Handle (userId:Guid) = task {
                let! user = storage.GetUser(userId)
                
                match user with
                | null -> return "User not found" |> ResponseUtils.failedTyped<UserState>
                | _ -> return user.State |> ResponseUtils.success<UserState>
            }
            
            member this.Handle (email:string) = task {
                let! user = storage.GetUserByEmail(email)
                
                match user with
                | null -> return "User not found" |> ResponseUtils.failedTyped<UserState>
                | _ -> return user.State |> ResponseUtils.success<UserState>
            }
            
    module Brokerage =
        type Connect = struct end
    
        [<Struct>]
        type ConnectCallback = {
            Code:string
            UserId:Guid
        }
        
        [<Struct>]
        type Disconnect(userId:Guid) =
            member _.UserId = userId
            
        [<Struct>]
        type Info(userId:Guid) =
            member _.UserId = userId
    
        type Handler(accounts:IAccountStorage, brokerage:IBrokerage) = 
            
            interface IApplicationService
            
            member this.HandleConnect(_:Connect) = 
                brokerage.GetOAuthUrl()
                
            member this.HandleConnectCallback(cmd:ConnectCallback) = task {
                let! r = brokerage.ConnectCallback(cmd.Code)
                
                let! user = accounts.GetUser(cmd.UserId)
                
                user.ConnectToBrokerage(r.access_token, r.refresh_token, r.token_type, r.expires_in, r.scope, r.refresh_token_expires_in)
                
                do! accounts.Save(user)
                
                return ServiceResponse()
            }
            
            member this.HandleDisconnect(cmd:Disconnect) = task {
                let! user = accounts.GetUser(cmd.UserId)
                user.DisconnectFromBrokerage()
                do! accounts.Save(user)
                return ServiceResponse()
            }
            
            member this.HandleInfo(cmd:Info) = task {
                let! user = accounts.GetUser(cmd.UserId);

                let! oauth = brokerage.GetAccessToken(user.State)
                
                return ServiceResponse<OAuthResponse>(oauth)
            }