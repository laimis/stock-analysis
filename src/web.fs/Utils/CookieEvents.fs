namespace web.Utils

open System.Security.Claims
open System.Threading.Tasks
open core.fs.Accounts
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

type CookieEvents(logger: ILogger<CookieEvents>, service: Handler) =
    inherit CookieAuthenticationEvents()
    
    override this.RedirectToLogin(ctx: RedirectContext<CookieAuthenticationOptions>) =
        if ctx.Request.Path.StartsWithSegments("/api") then
            ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
        
        base.RedirectToLogin(ctx)
    
    override this.SigningIn(context: CookieSigningInContext) = task {
        let query = LookupByEmail(context.Principal.Email())
        
        let! response = service.Handle(query)
        
        match response with
        | Error _ ->
            logger.LogCritical($"Unable to look up user {query.Email} for sign in")
            raise (System.Exception("Failed to sign in via google"))
        | Ok result ->
            match context.Principal with
            | null -> 
                logger.LogCritical("Claims principal is null")
                raise (System.Exception("Failed to sign in via google"))
            | principal ->
                match principal.Identity with
                | :? ClaimsIdentity as identity ->
                    identity.AddClaim(
                        Claim(ID_CLAIM_NAME, result.Id.ToString())
                    )
                | _ ->
                    logger.LogCritical($"Claims principal is not a claims identity, it's a {principal.GetType().Name}")
                    raise (System.Exception("Failed to sign in via google"))
    }
    
    override this.ValidatePrincipal(context: CookieValidatePrincipalContext) = task {
        let query = LookupByEmail(context.Principal.Email())
        
        let! id = service.Handle(query)
        match id with
        | Error _ ->
            logger.LogCritical("Failed to validate principal")
            context.RejectPrincipal()
            do! context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
        | Ok _ -> ()
    }
