namespace web.Controllers

open System
open System.Security.Claims
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Accounts
open core.fs.Adapters.Email
open core.fs.Admin
open web.Utils

[<ApiController>]
[<Authorize("admin")>]
[<Route("api/[controller]")>]
type AdminController(handler: core.fs.Admin.Handler) =
    inherit ControllerBase()

    [<HttpGet("test")>]
    member this.Test() =
        this.Ok()

    [<HttpGet("loginas/{userId}")>]
    member this.LoginAs([<FromRoute>] userId: Guid, [<FromServices>] handler: core.fs.Accounts.Handler) : Task<ActionResult> = task {
        let! status = handler.Handle({LookupById.UserId = UserId(userId)})
        match status with
        | Error _ ->
            return this.NotFound() :> ActionResult
        | Ok user ->
            let claims = [
                Claim(ClaimTypes.GivenName, user.Firstname)
                Claim(ClaimTypes.Surname, user.Lastname)
                Claim(ClaimTypes.Email, user.Email)
                Claim(IdentityExtensions.ID_CLAIM_NAME, user.Id.ToString())
            ]
            let claimsIdentity = ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)
            let principal = ClaimsPrincipal(claimsIdentity)
            do! this.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)
            return this.Redirect("~/") :> ActionResult
    }

    [<HttpGet("delete/{userId}")>]
    member this.Delete([<FromRoute>] userId: Guid, [<FromServices>] service: core.fs.Accounts.Handler) : Task<ActionResult> =
        this.OkOrError(
            service.HandleDelete (UserId(userId)) {Delete.Feedback = ""}
        )

    [<HttpPost("email")>]
    member this.Email(obj: EmailInput) : Task<ActionResult> =
        this.OkOrError(handler.Handle({SendEmail.input = obj}))

    [<HttpGet("welcome")>]
    member this.Welcome([<FromQuery>] userId: Guid) : Task<ActionResult> =
        this.OkOrError(handler.Handle({SendWelcomeEmail.userId = UserId(userId)}))

    [<HttpGet("users")>]
    member this.ActiveAccounts() : Task<ActionResult> =
        this.OkOrError(handler.Handle({Query.everyone = true}))

    [<HttpGet("users/export")>]
    member this.Export() : Task<ActionResult> = task {
        let! result = handler.Handle(Unchecked.defaultof<Export>)
        return this.GenerateExport(result)
    }

    [<HttpGet("sec/ticker-sync")>]
    member this.TriggerSECTickerSync() : Task<ActionResult> =
        this.OkOrError(handler.Handle(Unchecked.defaultof<TriggerSECTickerSync>))

    [<HttpGet("sec/filings-migration")>]
    member this.MigrateSECFilings([<FromQuery>] userEmail: string) : Task<ActionResult> =
        this.OkOrError(handler.Handle({MigrateSECFilings.userEmail = userEmail}))
