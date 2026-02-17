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
    member this.Identity() = task {
        if this.User.Identity <> null && not this.User.Identity.IsAuthenticated then
            let view = AccountStatusView.notFound()
            return this.Ok(view) :> ActionResult
        else
            return! this.OkOrError(handler.Handle(LookupById(this.User.Identifier())))
    }

    [<HttpPost("validate")>]
    member this.Validate([<FromBody>] command: UserInfo) =
        if this.User.Identity <> null && this.User.Identity.IsAuthenticated then
            Task.FromResult<ActionResult>(this.BadRequest("User already has an account"))
        else
            this.OkOrError(handler.Validate(command))

    [<HttpPost>]
    member this.Create([<FromBody>] cmd: CreateAccount) = task {
        if this.User.Identity <> null && this.User.Identity.IsAuthenticated then
            return this.BadRequest("User already has an account") :> ActionResult
        else
            let! r = handler.Handle(cmd)
            if r.IsOk then
                do! AccountController.EstablishSignedInIdentity(this.HttpContext, r.ResultValue)
            return this.OkOrError(r)
    }

    [<HttpGet("login")>]
    [<Authorize>]
    member this.Login() =
        this.Redirect("~/")

    [<HttpGet("integrations/brokerage/connect")>]
    [<Authorize>]
    member this.Brokerage() = task {
        let! url = handler.Handle(Connect())
        return this.Redirect(url)
    }

    [<HttpGet("integrations/brokerage")>]
    [<Authorize>]
    member this.BrokerageInfo() =
        this.OkOrError(
            handler.HandleInfo(BrokerageInfo(this.User.Identifier()))
        )

    member private this.RedirectOrError(result: FSharpResult<'T, ServiceError>) =
        if result.IsError then
            this.BadRequest(result.ErrorValue.Message) :> ActionResult
        else
            this.Redirect("~/profile") :> ActionResult

    [<HttpGet("integrations/brokerage/disconnect")>]
    [<Authorize>]
    member this.BrokerageDisconnect() = task {
        let! result = handler.HandleDisconnect(BrokerageDisconnect(this.User.Identifier()))
        return this.RedirectOrError(result)
    }

    [<HttpGet("integrations/brokerage/callback")>]
    [<Authorize>]
    member this.BrokerageCallback([<FromQuery>] code: string) = task {
        let! result = handler.HandleConnectCallback(ConnectCallback(code, this.User.Identifier()))
        return this.RedirectOrError(result)
    }

    [<HttpPost("requestpasswordreset")>]
    member this.RequestPasswordReset([<FromBody>] cmd: RequestPasswordReset) =
        handler.Handle(cmd) |> ignore
        this.Ok()

    [<HttpPost("login")>]
    member this.Authenticate([<FromBody>] cmd: Authenticate) = task {
        let! response = handler.Handle(cmd)
        if response.IsOk then
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, response.ResultValue)
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
    member this.Delete([<FromBody>] cmd: Delete) = task {
        let! result = handler.HandleDelete(this.User.Identifier(), cmd)
        if result.IsError then
            return this.Error(result.ErrorValue)
        else
            do! this.HttpContext.SignOutAsync()
            return this.Ok() :> ActionResult
    }

    [<HttpPost("clear")>]
    [<Authorize>]
    member this.Clear() = task {
        do! handler.Handle(Clear(this.User.Identifier()))
        do! this.HttpContext.SignOutAsync()
        return this.Ok()
    }

    [<HttpPost("resetpassword")>]
    member this.ResetPassword([<FromBody>] cmd: ResetPassword) = task {
        let! result = handler.Handle(cmd)
        if result.IsOk then
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, result.ResultValue)
        return this.OkOrError(result)
    }

    [<HttpGet("confirm/{id}")>]
    member this.Confirm(id: Guid) = task {
        let! result = handler.Handle(Confirm(id))
        if result.IsOk then
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, result.ResultValue)
            return this.Redirect("~/")
        else
            return this.Error(result.ErrorValue)
    }

    [<HttpPost("settings")>]
    [<Authorize>]
    member this.Settings([<FromBody>] cmd: SetSetting) =
        this.OkOrError(handler.HandleSettings(this.User.Identifier(), cmd))

    [<HttpGet("transactions")>]
    member this.GetTransactions() =
        this.OkOrError(
            handler.Handle(GetAccountTransactions(this.User.Identifier()))
        )

    [<HttpPost("transactions/{transactionId}/applied")>]
    member this.ApplyTransaction(transactionId: string) =
        this.OkOrError(
            handler.Handle(MarkAccountTransactionAsApplied(this.User.Identifier(), transactionId))
        )
