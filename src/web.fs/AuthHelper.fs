namespace web

open System.Security.Claims
open System.Threading.Tasks
open Hangfire.Dashboard
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open web.Utils

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
        member this.Authorize(context: DashboardContext) =
            let httpContext = context.GetHttpContext()
            match httpContext.User.Identity with
            | null -> false
            | identity -> identity.IsAuthenticated
