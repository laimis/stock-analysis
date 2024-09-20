namespace core.fs.Accounts

    open System
    open System.ComponentModel.DataAnnotations
    open core.Account
    open core.fs
    open core.fs.Adapters.Authentication
    open core.fs.Adapters.Brokerage
    open core.fs.Adapters.Email
    open core.fs.Adapters.Storage
    open core.fs.Adapters.Subscriptions
    
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
            BrokerageAccessTokenExpirationDate: DateTimeOffset
            BrokerageRefreshTokenExpirationDate: DateTimeOffset
            MaxLoss: decimal option
            InterestReceived: decimal
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
                BrokerageAccessTokenExpirationDate = state.BrokerageAccessTokenExpires
                BrokerageRefreshTokenExpirationDate = state.BrokerageRefreshTokenExpires
                MaxLoss = state.MaxLoss
                InterestReceived = state.InterestReceived 
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
                BrokerageAccessTokenExpirationDate = DateTimeOffset.MinValue
                BrokerageRefreshTokenExpirationDate = DateTimeOffset.MinValue
                MaxLoss = None
                InterestReceived = 0.0m
            }    
    
    [<CLIMutable>]
    [<Struct>]
    type Authenticate = {
            [<Required>]
            Email: string
            [<Required>]
            Password: string
        }
    
    [<Struct>]
    type Clear = {
        UserId: UserId
    }
    
    [<Struct>]
    type GetAccountTransactions = {
        UserId: UserId
    }
    
    [<Struct>]
    type MarkAccountTransactionAsApplied = {
        UserId: UserId
        TransactionId: string
    }
    
    [<Struct>]
    type Delete = {
        Feedback: string
    }
        
    [<Struct>]
    type Confirm = {
        Id: Guid
    }
    
    [<CLIMutable>]
    [<Struct>]
    type Contact = {
        [<Required>]
        Email: string
        [<Required>]
        Message: string
    }
    
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
    type CreateAccount = {
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
        
    [<CLIMutable>]
    [<Struct>]
    type RequestPasswordReset = {
        [<Required>]
        Email: string
    }
    
    type LookupByEmail = {
        Email:string
    }
    
    type LookupById = {
        UserId:UserId
    }
    
    type Connect = struct end
        
    type RefreshConnection = {
        UserId:UserId
    }

    [<Struct>]
    type ConnectCallback = {
        Code:string
        UserId:UserId
    }
    
    [<Struct>]
    type BrokerageDisconnect =
        {
            UserId:UserId
        }
        
    [<Struct>]
    type BrokerageInfo =
        {
            UserId:UserId
        }
        
    type SetSetting =
        {
            [<Required>]
            Key: string
            [<Required>]
            Value: string
        }
        
    type Handler(
        brokerage:IBrokerage,
        email:IEmailService,
        storage:IAccountStorage,
        hashProvider:IPasswordHashProvider,
        portfolioStorage:IPortfolioStorage,
        roles:IRoleService,
        subscriptions:ISubscriptions) =
        let INVALID_EMAIL_PASSWORD = "Invalid email/password combination"
    
        let success (user:User) =
            user.State |> AccountStatusView.fromUserState (roles.IsAdmin(user.State)) |> Ok
            
        let successOrNotFound (user:User option) =
            match user with
                | None -> AccountStatusView.notFound() |> Ok 
                | Some user -> user |> success
                
        let attemptLogin (user:User) (command:Authenticate) = task {
            
            let validPassword =
                hashProvider.GenerateHash command.Password (user.State.GetSalt())
                |> user.PasswordHashMatches
            
            match validPassword with
            | false -> return INVALID_EMAIL_PASSWORD |> ServiceError |> Error
            | true -> return user |> success
        }
        
        let processPaymentInfo user paymentInfo =
            let result = subscriptions.Create user paymentInfo.Token.Email paymentInfo.PlanId paymentInfo.Token.Id
            
            match result.CustomerId with
            | null ->
                $"Failed to process the payment, please try again or use a different payment form" |> ServiceError |> Error
            | _ ->
                user.SubscribeToPlan paymentInfo.PlanId result.CustomerId result.SubscriptionId
                Ok ()
        
        interface IApplicationService
        
        member this.Handle (query:GetAccountTransactions) = task {
            let! user = storage.GetUser(query.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some _ ->
                let! transactions = storage.GetAccountBrokerageTransactions query.UserId
                return transactions |> Ok
        }
        member this.Handle (cmd:MarkAccountTransactionAsApplied) = task {
            let! user = storage.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some _ ->
                let! transactions = storage.GetAccountBrokerageTransactions cmd.UserId
                let transaction = transactions |> Seq.find (fun (t:AccountTransaction) -> t.TransactionId = cmd.TransactionId)
                let applied = {transaction with Applied = DateTimeOffset.UtcNow |> Some}
                do! [|applied|] |> storage.SaveAccountBrokerageTransactions cmd.UserId 
                return Ok ()
        }
        member this.Handle (command:Authenticate) = task {
            let! user = storage.GetUserByEmail(command.Email)
            
            match user with
            | None -> return INVALID_EMAIL_PASSWORD |> ServiceError |> Error
            | Some user -> return! command |> attemptLogin user
        }
        
        member this.Handle (command:Clear) = task {
            let! user = storage.GetUser(command.UserId)
            
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                do! portfolioStorage.Delete(user.Id |> UserId)
                return Ok ()
        }
        
        member this.HandleDelete userId (command:Delete)  = task {
            let! user = storage.GetUser(userId)
            
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                user.Delete(command.Feedback)
                do! storage.Save(user)
                
                // TODO: technically it would be better to mark the user as deleted and then have a background process
                // to send emails, delete the actual user record, and delete portfolio data
                let properties = {|feedback = command.Feedback; email = user.State.Email;|}
                do! email.SendWithTemplate (Recipient(email=roles.GetAdminEmail(), name="Admin")) Sender.NoReply EmailTemplate.AdminUserDeleted properties
                    
                    
                do! storage.Delete(user)
                do! portfolioStorage.Delete(user.Id |> UserId)
                return Ok ()
        }
        member this.Handle (command:Confirm) = task {
            let! association = storage.GetUserAssociation(command.Id)
            match association with
            | None -> return "Invalid confirmation identifier." |> ServiceError |> Error
            | Some association ->
                let! user = storage.GetUser(association.UserId)
                match user with
                | None -> return "User not found" |> ServiceError |> Error
                | Some user ->
                    
                    match association.IsOlderThan(TimeSpan.FromDays(30)) with
                    | true -> return "Confirmation link is expired. Please request a new one." |> ServiceError |> Error
                    | false ->
                        user.Confirm()
                        do! storage.Save(user)
                        do! email.SendWithTemplate (Recipient(email = user.State.Email, name = user.State.Name)) Sender.Support EmailTemplate.NewUserWelcome {||}
                        return user |> success
        }
    
        member this.Handle (command:Contact) : System.Threading.Tasks.Task<Result<Unit,ServiceError>> = task {
            do! email.SendWithTemplate
                    (Recipient(email=roles.GetAdminEmail(), name=null))
                    Sender.NoReply
                    EmailTemplate.AdminContact
                    {|message = command.Message; email = command.Email|}
            return Ok ()
        }
        
        member this.Handle (command:CreateAccount) = task {
            
            let! exists = storage.GetUserByEmail(command.UserInfo.Email)
            match exists with
            | Some _ -> return $"Account with {command.UserInfo.Email} already exists" |> ServiceError |> Error
            | None ->
                let u = User.Create(email=command.UserInfo.Email, firstname=command.UserInfo.FirstName, lastname=command.UserInfo.LastName)
                
                let hashAndSalt = hashProvider.GenerateHashAndSalt command.UserInfo.Password 32
                
                u.SetPassword hashAndSalt.Hash hashAndSalt.Salt
                
                let paymentResponse = command.PaymentInfo |> processPaymentInfo u
                
                match paymentResponse with
                | Error err ->
                    return err.Message |> ServiceError |> Error
                | _ ->
                    do! storage.Save(u)
                    return u |> success
        }
        
        member this.Validate (userInfo:UserInfo) = task {
            let! exists = storage.GetUserByEmail(userInfo.Email)
            match exists with
            | None -> return Ok ()
            | Some _ -> return $"Account with {userInfo.Email} already exists" |> ServiceError |> Error
        }
        
        member this.Handle (reset:ResetPassword) = task {
            let! association = storage.GetUserAssociation(reset.Id)
            match association with
            | None -> return "Invalid password reset token. Check the link in the email or request a new password reset." |> ServiceError |> Error
            | Some association ->
                let! user = association.UserId |>  storage.GetUser
                match user with
                | None -> return "User account is no longer valid" |> ServiceError |> Error
                | Some user ->
                    match association.IsOlderThan(TimeSpan.FromMinutes(15)) with
                    | true -> return "Password reset link is expired. Please request a new one." |> ServiceError |> Error
                    | false ->
                        let hashAndSalt = hashProvider.GenerateHashAndSalt reset.Password 32
                        user.SetPassword hashAndSalt.Hash hashAndSalt.Salt
                        do! storage.Save(user)
                        return user |> success
        }
        
        member this.Handle (createNotifications:SendCreateNotifications) = task {
            
            let! user = storage.GetUser(createNotifications.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                let request = ProcessIdToUserAssociation(createNotifications.UserId, createNotifications.Created)
                
                do! storage.SaveUserAssociation(request)
                
                let confirmUrl = $"{EmailSettings.ConfirmAccountUrl}/{request.Id}"
                
                do! email.SendWithTemplate (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply EmailTemplate.ConfirmAccount {|confirmurl = confirmUrl|}
                do! email.SendWithTemplate (Recipient(email=roles.GetAdminEmail(), name="Admin")) Sender.NoReply EmailTemplate.AdminNewUser {|email = user.State.Email; |}
                
                return Ok ()
        }
    
        member this.Handle (request:RequestPasswordReset) = task {
            let! user = storage.GetUserByEmail(request.Email)
            
            match user with
            | None -> return () // don't return an error so that user accounts can't be enumerated
            | Some user ->
                let association = ProcessIdToUserAssociation(user.Id |> UserId, DateTimeOffset.UtcNow)
                
                do! storage.SaveUserAssociation(association)
                
                let resetUrl = $"{EmailSettings.PasswordResetUrl}/{association.Id}"
                
                do! email.SendWithTemplate (Recipient(email=user.State.Email, name=user.State.Name)) Sender.NoReply EmailTemplate.PasswordReset {|reseturl = resetUrl|}
                
                return ()
        }
        
        member this.Handle (query:LookupById) : System.Threading.Tasks.Task<Result<AccountStatusView,ServiceError>> = task {
            let! user = storage.GetUser(query.UserId)
            return user |> successOrNotFound
        }
        
        member this.Handle (query:LookupByEmail) : System.Threading.Tasks.Task<Result<AccountStatusView,ServiceError>> = task {
            let! user = storage.GetUserByEmail(query.Email)
            return user |> successOrNotFound
        }
            
    
        member this.Handle(_:Connect) = brokerage.GetOAuthUrl()
            
        member this.HandleConnectCallback(cmd:ConnectCallback) = task {
            let! connectResponse = brokerage.ConnectCallback(cmd.Code)
            
            match connectResponse with
            | Error error -> return error |> Error
            | Ok accessToken ->
                let! user = storage.GetUser(cmd.UserId)
                match user with
                None -> return "User not found" |> ServiceError |> Error
                | Some user ->
                    user.ConnectToBrokerage accessToken.access_token accessToken.refresh_token accessToken.token_type accessToken.expires_in accessToken.scope
                    do! storage.Save(user)
                    return Ok ()
        }
        
        member this.HandleDisconnect(cmd:BrokerageDisconnect) = task {
            let! user = storage.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                user.DisconnectFromBrokerage()
                do! storage.Save(user)
                return Ok ()
        }
        
        member this.HandleInfo(cmd:BrokerageInfo) = task {
            let! user = storage.GetUser(cmd.UserId)
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                let! oauth = brokerage.GetAccessToken(user.State)
                
                return
                    match oauth.IsError with
                    | true -> oauth.error |> ServiceError |> Error
                    | false -> oauth |> Ok
        }
            
            
        member this.HandleSettings userId (command:SetSetting) = task {
            let! user = storage.GetUser(userId)
            
            match user with
            | None -> return "User not found" |> ServiceError |> Error
            | Some user ->
                user.SetSetting command.Key command.Value
                do! storage.Save(user)
                return user |> success
        }
