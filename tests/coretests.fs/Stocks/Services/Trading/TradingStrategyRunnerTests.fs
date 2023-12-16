module coretests.fs.Stocks.Services.Trading.TradingStrategyRunnerTests

open System
open System.Threading.Tasks
open Xunit
open core.Account
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Services.Trading
open core.fs.Stocks
open coretests.testdata
open timezonesupport
open FsUnit

let generatePriceBars numberOfBars priceFunction =
    
    let startingDate = DateTimeOffset.UtcNow.AddDays(-float numberOfBars)
    
    [|0..numberOfBars|]
    |> Array.map (fun i ->
        PriceBar(
            date = startingDate.AddDays(float i),
            ``open`` = priceFunction i,
            high = priceFunction i + 0.01m,
            low = priceFunction i - 0.01m,
            close = priceFunction i,
            volume = 1000
        )
    )
    |> PriceBars
    
let createRunner numberOfBars priceFunction =
    
    let prices = generatePriceBars numberOfBars priceFunction

    let mock =
        {
            new IBrokerageGetPriceHistory with 
                member this.GetPriceHistory userState ticker priceFrequency start ``end`` =
                    prices |> ServiceResponse<PriceBars> |> Task.FromResult
        }
    
    TradingStrategyRunner(mock, MarketHoursAlwaysOn()), prices

[<Fact>]
let ``Basic test``() = task {
    
    let runner, prices = createRunner 100 (fun i -> 10 + i |> decimal)

    let! results = 
        runner.Run(
            UserState(),
            100,
            10,
            Some 5m,
            TestDataGenerator.NET,
            prices.First.Date,
            false
        )

    let oneThirdResult = results.Results[0]

    let maxDrawdown = oneThirdResult.MaxDrawdownPct
    let maxGain = oneThirdResult.MaxGainPct
    let position = oneThirdResult.Position

    position.IsClosed |> should equal true
    position.Profit |> should equal 1005
    position.GainPct |> should equal 1.005m
    position.RR |> should equal 2.01m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 1.501m
    position.DaysHeld |> should equal 15

    let oneThirdPercentBased = results.Results[1]
    let maxDrawdown = oneThirdPercentBased.MaxDrawdownPct
    let maxGain = oneThirdPercentBased.MaxGainPct
    let position = oneThirdPercentBased.Position

    position.IsClosed |> should equal true
    position.Profit |> should equal 137.3m
    position.GainPct |> should equal 0.1373m
    position.RR |> should equal 0.2746m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 0.201m
    position.DaysHeld |> should equal 2
}

[<Fact>]
let ``With portion size too small, still sells at RR levels``() =
    
    let bars = generatePriceBars 100 (fun i -> 10 + i |> decimal)

    let runner = TradingStrategyFactory.createProfitPointsTrade 3
    
    let result =
        StockPosition.openLong TestDataGenerator.NET bars.First.Date
        |> StockPosition.buy 2m 10m  bars.First.Date None
        |> StockPosition.setStop (Some 5m)  bars.First.Date
        |> runner.Run bars false
    
    let maxDrawdown = result.MaxDrawdownPct
    let maxGain = result.MaxGainPct
    let position = result.Position
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 15
    position.GainPct |> should equal 0.75m
    position.RR |> should equal 1.5m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 1.001m
    position.DaysHeld |> should equal 10

[<Fact>]
let ``With position not fully sold, is open``() = task {
    let runner, prices = createRunner 100 (fun i -> 10 + i |> decimal)

    let! result = runner.Run(
        UserState(),
        numberOfShares = 2,
        price = 50,
        stopPrice = Some 0.01m,
        ticker = TestDataGenerator.NET,
        ``when``= prices.First.Date,
        closeIfOpenAtTheEnd=false)

    result.Results
    |> Seq.take 1
    |> Seq.iter (fun r ->
        r.Position.IsClosed |> should equal false
    )
}

let createDownsideTestData() =
    
        let bars = generatePriceBars 10 (fun i -> 50 - i |> decimal)
    
        let positionInstance = 
            StockPosition.openLong TestDataGenerator.NET bars.First.Date
            |> StockPosition.buy 5m 50m bars.First.Date None
            |> StockPosition.setStop (Some 45m) bars.First.Date
        
        (bars, positionInstance)
        
[<Fact>]
let ``With price falling, stop price exit executes``() =
    
    let bars, positionInstance = createDownsideTestData()
    let runner = TradingStrategyFactory.createProfitPointsTrade 3
    let result = runner.Run bars false positionInstance
    
    let position = result.Position
    let maxGain = result.MaxGainPct
    let maxDrawdown = result.MaxDrawdownPct
    
    position.IsClosed |> should equal true
    position.Profit |> should equal -25
    position.GainPct |> should equal -0.1m
    position.RR |> should equal -1m
    position.DaysHeld |> should equal 5
    maxGain |> should equal 0.0002m
    maxDrawdown |> should equal -0.1002m

[<Fact>]
let ``Close after fixed number of days, works``() =
    
    let data = generatePriceBars 10 (fun i -> 50 + i |> decimal)

    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date None
        |> StockPosition.setStop (Some 45m) data.First.Date
        
    let runner = TradingStrategyFactory.createCloseAfterFixedNumberOfDays 5
    
    let result = runner.Run data false positionInstance
    
    let position = result.Position
    let maxGain = result.MaxGainPct
    let maxDrawdown = result.MaxDrawdownPct
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 30
    position.GainPct |> should equal 0.12m
    position.RR |> should equal 1.2m
    position.DaysHeld |> should equal 6
    maxGain |> should equal 0.1202m
    maxDrawdown |> should equal -0.0002m