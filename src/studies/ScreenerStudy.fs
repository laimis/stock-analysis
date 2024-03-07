module studies.ScreenerStudy

open FSharp.Data
open studies.DataHelpers
open studies.Types
open Microsoft.Extensions.Logging

let run() =
    
    let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()

    let studiesDirectory = ServiceHelper.getArgumentValue "-d"
        
    let actions = [
        if ServiceHelper.hasArgument "-i" then fun () -> async {
            let importUrl = ServiceHelper.getArgumentValue "-i"
            let! response = Http.AsyncRequest(importUrl)
            let csv =
                match response.Body with
                | Text text -> text
                | _ -> failwith "Unexpected response from screener"
            let outputFilename = ServiceHelper.getArgumentValue "-o"
            do! csv |> saveCsv outputFilename
        }
        
        if ServiceHelper.hasArgument "-pt" then fun () -> async {
            
            let brokerage = ServiceHelper.brokerage()    
            let pricesWrapper =
                {
                    new IGetPriceHistory with 
                        member this.GetPriceHistory start ``end`` ticker =
                            brokerage.GetPriceHistory user.Value.State ticker core.fs.Adapters.Stocks.PriceFrequency.Daily start ``end``
                }
            
            let! transformed =
                ServiceHelper.getArgumentValue "-f"
                |> Signal.Load |> _.Rows
                |> PriceTransformation.transform pricesWrapper studiesDirectory
                
            let outputFilename = ServiceHelper.getArgumentValue "-o"
            do! transformed.SaveToString() |> appendCsv outputFilename
        }
    ]

    actions
        |> List.map (fun a -> a())
        |> Async.Sequential
        |> Async.RunSynchronously
        |> ignore
