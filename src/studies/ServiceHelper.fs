module studies.ServiceHelper

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Storage
open web

type EnvironmentContext(host:IHost, logger, commandLine) =
    member this.GetArgumentValue switch =
        let index = commandLine |> Array.tryFindIndex (fun arg -> arg = switch)
        match index with
        | Some i -> commandLine[i+1]
        | None -> failwith $"No value specified for {switch}"
        
    member this.HasArgument switch = commandLine |> Array.exists (fun arg -> arg = switch)

    member this.Host = host
    member this.Logger = logger
    member this.CommandLine = commandLine
    
    member this.Storage() = host.Services.GetService(typeof<IAccountStorage>) :?> IAccountStorage
    member this.Brokerage() = host.Services.GetService(typeof<IBrokerage>) :?> IBrokerage

let mutable logger : ILogger = null
    
let init (configuration:IConfiguration option) args =
    
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
    let newLogger = loggerFactory.CreateLogger("study")
    logger <- newLogger
    
    let builder = Host.CreateApplicationBuilder args
    
    let configuration =
        match configuration with
        | None -> builder.Configuration :> IConfiguration
        | Some config -> config
     
    DIHelper.RegisterServices(
        configuration,
        builder.Services,
        newLogger
    )

    let host = builder.Build()
    
    let context = EnvironmentContext(host, logger, args)
    
    context
