namespace web.Controllers

open System
open System.Threading.Tasks
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
    member this.LoginAs([<FromRoute>] userId: Guid, [<FromServices>] handler: core.fs.Accounts.Handler) = task {
        let! status = handler.Handle(LookupById(UserId.NewUserId(userId)))
        if status.IsError then
            return this.NotFound() :> ActionResult
        else
            do! AccountController.EstablishSignedInIdentity(this.HttpContext, status.ResultValue)
            return this.Redirect("~/")
    }

    [<HttpGet("delete/{userId}")>]
    member this.Delete([<FromRoute>] userId: Guid, [<FromServices>] service: core.fs.Accounts.Handler) =
        this.OkOrError(
            service.HandleDelete(UserId.NewUserId(userId), Delete(None))
        )

    [<HttpPost("email")>]
    member this.Email(obj: EmailInput) =
        this.OkOrError(handler.Handle(SendEmail(obj)))

    [<HttpGet("welcome")>]
    member this.Welcome([<FromQuery>] userId: Guid) =
        this.OkOrError(handler.Handle(SendWelcomeEmail(UserId.NewUserId(userId))))

    [<HttpGet("users")>]
    member this.ActiveAccounts() =
        this.OkOrError(handler.Handle(Query(true)))

    [<HttpGet("users/export")>]
    member this.Export() = task {
        let! result = handler.Handle(Export())
        return this.GenerateExport(result)
    }

    [<HttpGet("sec/ticker-sync")>]
    member this.TriggerSECTickerSync() =
        this.OkOrError(handler.Handle(TriggerSECTickerSync()))
