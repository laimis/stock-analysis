module studies.ServiceHelper

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Storage
open web

let mutable host = null
let mutable logger : ILogger = null

let init args =
    
    let loggerFactory = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore)
    logger <- loggerFactory.CreateLogger("study")

    let builder = Host.CreateApplicationBuilder args
     
    DIHelper.RegisterServices(
        builder.Configuration,
        builder.Services,
        logger
    )

    host <- builder.Build()
    
let storage() = host.Services.GetService(typeof<IAccountStorage>) :?> IAccountStorage
let brokerage() = host.Services.GetService(typeof<IBrokerage>) :?> IBrokerage
