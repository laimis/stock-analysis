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

Environment.GetCommandLineArgs() |> ServiceHelper.init

let user = "laimis@gmail.com" |> getUser (ServiceHelper.storage()) |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let inputFilename = "d:\studies\01_export_date_ticker_screenerid.csv"

let priceFunction = getPrices user.Value (ServiceHelper.brokerage())

GapStudy.run inputFilename priceFunction |> Async.RunSynchronously