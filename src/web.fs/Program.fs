namespace web

open System
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Diagnostics
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open Hangfire
open Hangfire.PostgreSql
open web.Utils

type Program() =
    static member Main(args: string[]) =
        let builder = WebApplication.CreateBuilder(args)
        
        let configuration = builder.Configuration
        
        use loggerFactory = LoggerFactory.Create(fun loggingBuilder -> loggingBuilder.AddConsole() |> ignore)
        let logger = loggerFactory.CreateLogger(typeof<Program>.FullName)
        
        AuthHelper.Configure(configuration, builder.Services)
        
        Jobs.AddJobs(configuration, builder.Services, logger)
        
        builder.Services
            .AddControllers(fun o ->
                o.InputFormatters.Add(TextPlainInputFormatter())
            )
            .AddJsonOptions(fun o ->
                for type' in typeof<Program>.Assembly.GetTypes() do
                    if type'.IsSubclassOf(typeof<JsonConverter>) && not type'.IsAbstract then
                        let converter = Activator.CreateInstance(type') :?> JsonConverter
                        o.JsonSerializerOptions.Converters.Add(converter)
            ) |> ignore
        
        builder.Services.Configure<ForwardedHeadersOptions>(fun (options: ForwardedHeadersOptions) ->
            options.ForwardedHeaders <-
                ForwardedHeaders.XForwardedFor |||
                ForwardedHeaders.XForwardedProto
            
            options.KnownIPNetworks.Clear()
            options.KnownProxies.Clear()
        ) |> ignore
        
        builder.Services.AddHealthChecks()
            .AddCheck<HealthCheck>("storage based health check")
            |> ignore
        
        di.DIHelper.registerServices(configuration, builder.Services, logger)
        builder.Services.AddSingleton<CookieEvents>() |> ignore
        
        let cnn = configuration.GetValue<string>("DB_CNN")
        if not (String.IsNullOrEmpty(cnn)) then
            GlobalConfiguration.Configuration.UsePostgreSqlStorage(fun opt ->
                opt.UseNpgsqlConnection(cnn) |> ignore
            ) |> ignore
        
        let app = builder.Build()
        
        if app.Environment.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseForwardedHeaders() |> ignore
            
            app.UseExceptionHandler(fun exceptionHandlerApp ->
                exceptionHandlerApp.Run(fun context -> task {
                    context.Response.StatusCode <- StatusCodes.Status500InternalServerError
                    context.Response.ContentType <- "text/plain"
                    
                    let feature = context.Features.Get<IExceptionHandlerFeature>()
                    
                    do! context.Response.WriteAsync(feature.Error.Message)
                })
            ) |> ignore
            
            app.UseHsts() |> ignore
        
        let staticFileExtensionProvider = FileExtensionContentTypeProvider()
        staticFileExtensionProvider.Mappings.[".avif"] <- "image/avif"
        
        let staticFileOptions = StaticFileOptions()
        staticFileOptions.ContentTypeProvider <- staticFileExtensionProvider
        staticFileOptions.OnPrepareResponse <- fun ctx ->
            if ctx.File.Name = "index.html" then
                ctx.Context.Response.Headers.["Cache-Control"] <- StringValues("no-cache, no-store")
                ctx.Context.Response.Headers.["Expires"] <- StringValues("-1")
        
        app.UseStaticFiles(staticFileOptions) |> ignore
        
        app.UseRouting() |> ignore
        
        app.UseAuthentication() |> ignore
        app.UseAuthorization() |> ignore
        
        app.MapHealthChecks("/health") |> ignore
        app.MapControllerRoute("default", "api/{controller=Home}/{action=Index}/{id?}") |> ignore
        app.MapHangfireDashboard("/hangfire", DashboardOptions(Authorization = [| MyAuthorizationFilter() :> IDashboardAuthorizationFilter |])) |> ignore
        app.MapFallbackToFile("index.html") |> ignore
        
        let appLogger = app.Services.GetRequiredService<ILogger<Program>>()
        Jobs.ConfigureJobs(app, appLogger)
        
        app.Run()

[<EntryPoint>]
let main args =
    Program.Main(args)
    0
