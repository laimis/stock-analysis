// set up logging
// let verbose = args |> Array.contains "-v"

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open storage.shared
open web
open web.Utils

let loggerFactory = LoggerFactory.Create(fun builder ->
    builder.AddConsole() |> ignore
    match true with
    | true -> builder.SetMinimumLevel(LogLevel.Trace) |> ignore
    | false -> builder.SetMinimumLevel(LogLevel.Error) |> ignore
)
let logger = loggerFactory.CreateLogger("migrations")

let builder = System.Environment.GetCommandLineArgs() |> Host.CreateApplicationBuilder 
 
DIHelper.RegisterServices(
    builder.Configuration,
    builder.Services,
    logger
)

let host = builder.Build()

let storage = host.Services.GetService(typeof<IPortfolioStorage>) :?> PortfolioStorage
let accounts = host.Services.GetService(typeof<IAccountStorage>) :?> IAccountStorage

logger.LogInformation("Starting migrations")

let wrapperLogging = {
    new ILogger with
        member _.LogInformation message = logger.LogInformation(message)
        member _.LogError message = logger.LogError(message)
        member _.LogWarning message = logger.LogWarning(message)
}

migrations.MigrateFromV2ToV3.migrate storage accounts wrapperLogging |> Async.AwaitTask |> Async.RunSynchronously |> ignore

logger.LogInformation("Finished migrations")
