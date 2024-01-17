module studies.ServiceHelper

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Storage
open web

let mutable host = null
let mutable logger : ILogger = null
let mutable private commandLine:string[] = null

let studiesDirectory() =
    commandLine |> Array.tryFindIndex (fun arg -> arg = "-d") |> Option.map (fun i -> commandLine[i+1])    

let hasArgument switch = commandLine |> Array.exists (fun arg -> arg = switch)

let hasImportUrl() = commandLine |> Array.exists (fun arg -> arg = "-i")

let importUrl() =
    commandLine |> Array.findIndex (fun arg -> arg = "-i") |> fun i -> commandLine[i+1]
    
let outputFilename() =
    let index = commandLine |> Array.tryFindIndex (fun arg -> arg = "-o")
    match index with
    | Some i -> commandLine[i+1]
    | None -> failwith "No output file specified, use -o <filename>"
    
let inputFilename() =
    let index = commandLine |> Array.tryFindIndex (fun arg -> arg = "-f")
    match index with
    | Some i -> commandLine[i+1]
    | None -> failwith "No input file specified, use -f <filename>"

let init args =
    
    // print args
    args |> Array.iter (fun arg -> printfn $"%s{arg}")
    commandLine <- args
    
    // set up logging
    let verbose = args |> Array.contains "-v"
    let loggerFactory = LoggerFactory.Create(fun builder ->
        builder.AddConsole() |> ignore
        match verbose with
        | true -> builder.SetMinimumLevel(LogLevel.Trace) |> ignore
        | false -> builder.SetMinimumLevel(LogLevel.Error) |> ignore
    )
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
