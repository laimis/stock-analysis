namespace web

open System
open System.Security.Claims
open System.Threading.Tasks
open Hangfire.Dashboard
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open web.Utils
open core.fs.Accounts

type CookieEvents(logger: ILogger<CookieEvents>, service: Handler) =
    inherit CookieAuthenticationEvents()
    
    override _.RedirectToLogin(ctx: RedirectContext<CookieAuthenticationOptions>) =
        if ctx.Request.Path.StartsWithSegments(PathString("/api")) then
            ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized
        base.RedirectToLogin ctx
    
    override _.SigningIn(context: CookieSigningInContext) =
        task {
            let query = { LookupByEmail.Email = context.Principal.Email() }
            let! response = service.Handle query
            match response with
            | Error _ ->
                logger.LogCritical $"Unable to look up user {query.Email} for sign in"
                raise (Exception "Failed to sign in via google")
            | Ok view ->
                match context.Principal.Identity with
                | :? ClaimsIdentity as identity ->
                    identity.AddClaim(Claim(ID_CLAIM_NAME, view.Id.ToString()))
                | _ ->
                    let identityType = if isNull context.Principal.Identity then "null" else context.Principal.Identity.GetType().Name
                    logger.LogCritical("Claims principal identity is not a ClaimsIdentity, it's a {identity}", identityType)
                    raise (Exception "Failed to sign in via google")
        } :> Task
    
    override _.ValidatePrincipal(context: CookieValidatePrincipalContext) =
        task {
            let query = { LookupByEmail.Email = context.Principal.Email() }
            let! result = service.Handle query
            match result with
            | Error _ ->
                logger.LogCritical "Failed to validate principal"
                context.RejectPrincipal()
                do! context.HttpContext.SignOutAsync CookieAuthenticationDefaults.AuthenticationScheme
            | Ok _ -> ()
        } :> Task

type AuthHelper() =
    static member Configure(configuration: IConfiguration, services: IServiceCollection) =
        let adminEmail = configuration.GetValue<string>("ADMINEmail")
        if System.String.IsNullOrWhiteSpace(adminEmail) then
            raise (System.Exception("ADMINEmail is not set"))
        
        let authBuilder =
            services
                .AddAuthentication(fun options ->
                    options.DefaultAuthenticateScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    options.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    options.DefaultChallengeScheme <- "Google"
                )
                .AddCookie(fun opt ->
                    opt.Cookie.Name <- "tw"
                    opt.EventsType <- typeof<CookieEvents>
                )
        
        let googleClientId = configuration.GetValue<string>("GoogleClientId")
        if not (isNull googleClientId) then
            authBuilder.AddGoogle("Google", fun options ->
                options.ClientId <- googleClientId
                options.ClientSecret <- configuration.GetValue<string>("GoogleSecret")
                options.ReturnUrlParameter <- "returnUrl"
                let authEndpoint = options.Events.OnRedirectToAuthorizationEndpoint
                
                options.Events.OnRedirectToAuthorizationEndpoint <- fun context ->
                    if context.Request.Path.StartsWithSegments(PathString("/api")) then
                        context.Response.StatusCode <- StatusCodes.Status401Unauthorized
                        Task.CompletedTask
                    else
                        authEndpoint.Invoke(context)
            ) |> ignore
        
        services.AddAuthorization(fun opt ->
            opt.AddPolicy("admin", fun p -> p.RequireClaim(ClaimTypes.Email, adminEmail) |> ignore)
        ) |> ignore

type MyAuthorizationFilter() =
    interface IDashboardAuthorizationFilter with
        member _.Authorize(context: DashboardContext) =
            let httpContext = context.GetHttpContext()
            let configurationObj = httpContext.RequestServices.GetService(typeof<IConfiguration>)
            if isNull configurationObj then
                false
            else
                let configuration = configurationObj :?> IConfiguration
                let adminEmail = configuration.GetValue<string>("ADMINEmail")
                match httpContext.User.Identity with
                | null -> false
                | identity when identity.IsAuthenticated && not (System.String.IsNullOrWhiteSpace(adminEmail)) ->
                    httpContext.User.HasClaim(ClaimTypes.Email, adminEmail)
                | _ -> false
