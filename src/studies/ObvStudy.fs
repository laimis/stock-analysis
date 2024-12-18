module studies.ObvStudy

open System
open MathNet.Numerics
open Microsoft.Extensions.Logging
open core.Account
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Reports
open core.fs.Services.Analysis
open studies.DataHelpers
open studies.ServiceHelper

type Trend = Rising | Falling | Sideways

type Divergence = {
    StartDate: string
    EndDate: string
    DivergenceType: string  // "Bullish" or "Bearish"
    PriceDelta: decimal
    ObvDelta: decimal
    Strength: decimal       // Measure of divergence strength
}

type ObvPoint = {
    Date: string
    Price: decimal
    Obv: decimal
}

// Function to filter significant divergences
let filterSignificantDivergences (divergences: Divergence list) (strengthThreshold: decimal) =
    divergences
    |> List.filter (fun d -> d.Strength >= strengthThreshold)
    |> List.sortByDescending (fun d -> d.Strength)
    
// Calculate slope for trend detection
let calculateSlope (values: decimal seq) (period: int) =
    let struct (a, b) =
        values
        |> Seq.mapi (fun i y -> (float i, float y))
        |> Seq.toArray
        |> fun points -> Fit.Line(Array.map fst points, Array.map snd points)

    b
    
// Determine trend based on slope
let determineTrend (slope: float) (threshold: float) =
    if slope > threshold then Rising
    elif slope < -threshold then Falling
    else Sideways
    
let findDivergences (report:DailyPositionBreakdown) period =
    callLogFuncIfSetup _.LogInformation("findDivergences")
    
    let detectDivergence period points =
        points
        |> List.windowed period
        |> List.mapi (fun i window ->
            let prices = window |> List.map (fun p -> p.Price)
            let obvs = window |> List.map (fun p -> p.Obv)
            
            let priceSlope = calculateSlope prices period
            let obvSlope = calculateSlope obvs period
            
            let divergenceStrength = abs(priceSlope - obvSlope)
            let threshold = 0.01  // Adjust based on sensitivity needs
            
            match determineTrend priceSlope threshold, determineTrend obvSlope threshold with
            | Rising, Falling ->
                Some {
                    StartDate = window[0].Date
                    EndDate = window[period-1].Date
                    DivergenceType = "Bearish"
                    PriceDelta = window[period-1].Price - window[0].Price
                    ObvDelta = window[period-1].Obv - window[0].Obv
                    Strength = decimal divergenceStrength
                }
            | Falling, Rising ->
                Some {
                    StartDate = window[0].Date
                    EndDate = window[period-1].Date
                    DivergenceType = "Bullish"
                    PriceDelta = window[period-1].Price - window[0].Price
                    ObvDelta = window[period-1].Obv - window[0].Obv
                    Strength = decimal divergenceStrength
                }
            | _ -> None
        )
        |> List.choose id

    let obvPoints =
        report.DailyObv.Data
        |> Seq.mapi (fun i (d:DataPoint<decimal>) -> {
            Date = d.Label
            Price = report.DailyClose.Data[i].Value
            Obv = d.Value
        })
        |> List.ofSeq
        
    let shortTermDivergences = detectDivergence period obvPoints
    
    shortTermDivergences

let analyzeDivergences period (report:DailyPositionBreakdown) =
    
    // Detect divergences for both 30-day and 100-day periods
    let allDivergences = 
        findDivergences report period
        |> filterSignificantDivergences <| 0.02M  // Adjust threshold as needed
    
    // Group divergences by type and sort by strength
    let (bullish, bearish) =
        allDivergences
        |> List.partition (fun d -> d.DivergenceType = "Bullish")

    // Return analysis results
    {|
        Divergences = allDivergences
        BullishDivergences = bullish
        BearishDivergences = bearish
        MostRecentDivergence = 
            allDivergences 
            |> List.sortByDescending (fun d -> d.EndDate) 
            |> List.tryHead
    |}
    
let runStudy (context:EnvironmentContext) (state:UserState) = async {
    let handler = context.Host.Services.GetService(typeof<core.fs.Reports.Handler>) :?> core.fs.Reports.Handler
    
    callLogFuncIfSetup _.LogInformation("was able to get it to work: " + handler.GetType().ToString())
    let startDate = "2024-03-28" |> Some
    let endDate = System.DateTimeOffset.Now.ToString("yyyy-MM-dd") |> Some
    let ticker = "-t" |> context.GetArgumentValue |> Ticker
    let period = "-p" |> context.GetArgumentValue |> int
    
    callLogFuncIfSetup _.LogInformation($"Running OBV study for {ticker} from {startDate.Value} to {endDate.Value}...")

    let userId = state.Id |> UserId
    
    let request = {DailyTickerReportQuery.Ticker = ticker; UserId = userId; StartDate = startDate; EndDate = endDate}

    let! report = handler.Handle(request) |> Async.AwaitTask
    
    match report with
    | Error er -> failwith er.Message
    | Ok report -> 
        
        let analysis = analyzeDivergences period report
    
        // let's print all the divergences
        analysis.Divergences |> List.sortBy _.EndDate |> List.iter (fun d ->
            // set the background color based on divergence type
            let color = if d.DivergenceType = "Bullish" then ConsoleColor.Green else ConsoleColor.Red
            Console.BackgroundColor <- color
            
            System.Console.WriteLine($"Divergence: {d.DivergenceType} {d.EndDate}")
            System.Console.WriteLine($"Price Delta: {d.PriceDelta}, Obv Delta: {d.ObvDelta}, Strength: {d.Strength}")
            System.Console.WriteLine()
            
            Console.ResetColor()
        )
        
        return ()
}

let run (context:EnvironmentContext) = async {  
    let! user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask
    match user with
    | None -> failwith "User not found"
    | Some user ->
        
        return! runStudy context user.State
}
