// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Adapters.Storage
open core.fs.Shared.Domain.Accounts
open studies
open web

let getUser (storage:IAccountStorage) email = async {
    return! storage.GetUserByEmail email |> Async.AwaitTask
}

let getPrices (user:User) (brokerage:IBrokerage) startDate endDate ticker = async {
    return! brokerage.GetPriceHistory user.State ticker PriceFrequency.Daily startDate endDate |> Async.AwaitTask
}

let loggerFactory = LoggerFactory.Create(fun builder -> builder.AddConsole() |> ignore)
let logger = loggerFactory.CreateLogger("GapStudy")

let builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs())
 
DIHelper.RegisterServices(
    builder.Configuration,
    builder.Services,
    logger
)

let host = builder.Build()
let storage = host.Services.GetService(typeof<IAccountStorage>) :?> IAccountStorage
let brokerage = host.Services.GetService(typeof<IBrokerage>) :?> IBrokerage

let user = "laimis@gmail.com" |> getUser storage |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let inputFilename = "d:\studies\01_export_date_ticker_screenerid.csv"

let func = getPrices user.Value brokerage

GapStudy.run inputFilename func |> Async.RunSynchronously