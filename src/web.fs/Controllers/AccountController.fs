namespace web.Controllers

open System
open System.Collections.Generic
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.FSharp.Core
open core.fs
open core.fs.Accounts
open web.Utils

[<ApiController>]
[<Route("api/[controller]")>]
type AccountController(handler: Handler) =
    inherit ControllerBase()

    static member EstablishSignedInIdentity(context: HttpContext, user: AccountStatusView) = task {
        let claims = [
            Claim(ClaimTypes.GivenName, user.Firstname)
            Claim(ClaimTypes.Surname, user.Lastname)
            Claim(ClaimTypes.Email, user.Email)
            Claim(IdentityExtensions.ID_CLAIM_NAME, user.Id.ToString())
        ]

        let claimsIdentity = ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
        let principal = ClaimsPrincipal(claimsIdentity)

        do! context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)
    }

    [<HttpGet("status")>]
    member this.Identity() : Task<ActionResult> = task {
        if this.User.Identity <> null && not this.User.Identity.IsAuthenticated then
            let view = AccountStatusView.notFound()
            return this.Ok(view) :> ActionResult
        else
            return! this.OkOrError(handler.Handle({LookupById.UserId = this.User.Identifier()}))
    }

    [<HttpPost("validate")>]
    member this.Validate([<FromBody>] command: UserInfo) =
        if this.User.Identity <> null && this.User.Identity.IsAuthenticated then
            Task.FromResult<ActionResult>(this.BadRequest("User already has an account"))
        else
            this.OkOrError(handler.Validate(command))

    [<HttpPost>]
    member this.Create([<FromBody>] cmd: CreateAccount) : Task<ActionResult> = task {
        if this.User.Identity <> null && this.User.Identity.IsAuthenticated then
            return this.BadRequest("User already has an account") :> ActionResult
        else
            let! r = handler.Handle(cmd)
            match r with
            | Ok user ->
                do! AccountController.EstablishSignedInIdentity(this.HttpContext, user)
            | Error _ -> ()
            return this.OkOrError(r)
    }

    [<HttpGet("login")>]
    [<Authorize>]
    member this.Login() =
        this.Redirect("~/")

    [<HttpGet("integrations/brokerage/connect")>]
    [<Authorize>]
    member this.Brokerage() : Task<ActionResult> = task {
        let! url = handler.Handle(Unchecked.defaultof<Connect>)
        return this.Redirect(url) :> ActionResult
    }

    [<HttpGet("integrations/brokerage")>]
    [<Authorize>]
    member this.BrokerageInfo() : Task<ActionResult> =
        this.OkOrError(
            handler.HandleInfo({BrokerageInfo.UserId = this.User.Identifier()})
        )

    member private this.RedirectOrError(result: Result<'T, ServiceError>) : ActionResult =
        match result with
        | Error err ->
            this.BadRequest(err.Message) :> ActionResult
        | Ok _ ->
            this.Redirect("~/profile") :> ActionResult

    [<HttpGet("integrations/brokerage/disconnect")>]
    [<Authorize>]
    member this.BrokerageDisconnect() : Task<ActionResult> = task {
        let! result = handler.HandleDisconnect({BrokerageDisconnect.UserId = this.User.Identifier()})
        return this.RedirectOrError(result)
    }

    [<HttpGet("integrations/brokerage/callback")>]
    [<Authorize>]
    member this.BrokerageCallback([<FromQuery>] code: string) : Task<ActionResult> = task {
        let! result = handler.HandleConnectCallback({ConnectCallback.Code = code; UserId = this.User.Identifier()})
        return this.RedirectOrError(result)
    }

    [<HttpPost("requestpasswordreset")>]
    member this.RequestPasswordReset([<FromBody>] cmd: RequestPasswordReset) =
        handler.Handle(cmd) |> ignore
        this.Ok()

    [<HttpPost("login")>]
    member this.Authenticate([<FromBody>] cmd: Authenticate) : Task<ActionResult> = task {
        let! response = handler.Handle(cmd)
        match response with
        | Ok user ->
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, user)
        | Error _ -> ()
        return this.OkOrError(response)
    }

    [<HttpPost("contact")>]
    member this.Contact([<FromBody>] cmd: Contact) =
        this.OkOrError(handler.Handle(cmd))

    [<HttpGet("logout")>]
    [<Authorize>]
    member this.LogoutAsync() = task {
        do! this.HttpContext.SignOutAsync()
        return this.Redirect("~/")
    }

    [<HttpPost("delete")>]
    [<Authorize>]
    member this.Delete([<FromBody>] cmd: Delete) : Task<ActionResult> = task {
        let! result = handler.HandleDelete (this.User.Identifier()) cmd
        match result with
        | Error err ->
            return this.Error(err)
        | Ok _ ->
            do! this.HttpContext.SignOutAsync()
            return this.Ok() :> ActionResult
    }

    [<HttpPost("clear")>]
    [<Authorize>]
    member this.Clear() : Task<ActionResult> = task {
        let! _ = handler.Handle({Clear.UserId = this.User.Identifier()})
        do! this.HttpContext.SignOutAsync()
        return this.Ok() :> ActionResult
    }

    [<HttpPost("resetpassword")>]
    member this.ResetPassword([<FromBody>] cmd: ResetPassword) : Task<ActionResult> = task {
        let! result = handler.Handle(cmd)
        match result with
        | Ok user ->
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, user)
        | Error _ -> ()
        return this.OkOrError(result)
    }

    [<HttpGet("confirm/{id}")>]
    member this.Confirm(id: Guid) : Task<ActionResult> = task {
        let! result = handler.Handle({Confirm.Id = id})
        match result with
        | Ok user ->
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, user)
            return this.Redirect("~/") :> ActionResult
        | Error err ->
            return this.Error(err)
    }

    [<HttpPost("settings")>]
    [<Authorize>]
    member this.Settings([<FromBody>] cmd: SetSetting) : Task<ActionResult> =
        this.OkOrError(handler.HandleSettings (this.User.Identifier()) cmd)

    [<HttpGet("transactions")>]
    member this.GetTransactions() : Task<ActionResult> =
        this.OkOrError(
            handler.Handle({GetAccountTransactions.UserId = this.User.Identifier()})
        )

    [<HttpPost("transactions/{transactionId}/applied")>]
    member this.ApplyTransaction(transactionId: string) : Task<ActionResult> =
        this.OkOrError(
            handler.Handle({MarkAccountTransactionAsApplied.UserId = this.User.Identifier(); TransactionId = transactionId})
        )
