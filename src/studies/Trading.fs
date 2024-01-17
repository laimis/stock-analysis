module studies.Trading

open core.fs.Adapters.Stocks
open studies.Types

let prepareSignalsForTradeSimulations (signalFilepath:string) (priceFunc:string -> Async<PriceBars>) = async {
    
    let signals =
        signalFilepath
        |> SignalWithPriceProperties.Load
        |> fun x -> x.Rows
        
    signals |> Seq.map Output |> Unified.describeRecords
    
    // ridiculous, sometimes data provider does not have prices for the date
    // so we filter those records out
    let! asyncData =
        signals
        |> Seq.map (fun r -> async {
            let! prices = r.Ticker |> priceFunc
            let startBar = r.Date |> prices.TryFindByDate
            return (r, prices, startBar)   
        })
        |> Async.Sequential
        
    let signalsWithPriceBars =
        asyncData
        |> Seq.choose (fun (r,prices,startBar) ->
            match startBar with
            | None -> None
            | Some _ -> Some (r, prices)
        )
    
    printfn "Ensured that data has prices"
    
    signalsWithPriceBars |> Seq.map fst |> Seq.map Output |> Unified.describeRecords
    
    return signalsWithPriceBars
}
    
let runTrades signalsWithPriceBars strategies = async {
    
    printfn "Executing trades..."
    
    let allOutcomes =
        signalsWithPriceBars
        |> Seq.map (fun signalWithPriceBars ->
            strategies |> Seq.map (fun strategy ->
                strategy signalWithPriceBars
            )
        )
        |> Seq.concat
        
    return allOutcomes
}