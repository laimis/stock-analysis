module studies.ServiceHelper

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Storage
open web

let mutable host = null
let mutable logger : ILogger = null
let mutable studiesDirectory = null

let init args =
    
    // print args
    args |> Array.iter (fun arg -> printfn $"%s{arg}")
    
    // set up logging
    let verbose = args |> Array.contains "-v"
    let loggerFactory = LoggerFactory.Create(fun builder ->
        builder.AddConsole() |> ignore
        match verbose with
        | true -> builder.SetMinimumLevel(LogLevel.Trace) |> ignore
        | false -> builder.SetMinimumLevel(LogLevel.Error) |> ignore
    )
    logger <- loggerFactory.CreateLogger("study")
    
    // find studies directory, it's passed in the arguments as -d directoryPath
    let studiesDir = args |> Array.tryFindIndex (fun arg -> arg = "-d") |> Option.map (fun i -> args.[i+1])
    match studiesDir with
    | Some dir -> 
        logger.LogInformation($"Using studies directory: {dir}")
        studiesDirectory <- dir
    | None ->
        logger.LogError("No studies directory specified, use -d directoryPath")
        exit 1

    let builder = Host.CreateApplicationBuilder args
     
    DIHelper.RegisterServices(
        builder.Configuration,
        builder.Services,
        logger
    )

    host <- builder.Build()
    
let storage() = host.Services.GetService(typeof<IAccountStorage>) :?> IAccountStorage
let brokerage() = host.Services.GetService(typeof<IBrokerage>) :?> IBrokerage
