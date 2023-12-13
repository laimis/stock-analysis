module coretests.fs.Stocks.Services.Trading.TradingStrategyRunnerTests

open System
open System.Threading.Tasks
open Xunit
open core.Account
open core.fs.Services.TradingStrategies
open core.fs.Shared
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Adapters.Stocks
open core.fs.Shared.Domain
open coretests.testdata
open timezonesupport
open FsUnit

let generatePriceBars numberOfBars priceFunction =
    
    [|0..numberOfBars|]
    |> Array.map (fun i ->
        PriceBar(
            date = DateTimeOffset.UtcNow.AddDays(float i),
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
    
    TradingStrategyRunner(mock, MarketHoursAlwaysOn())

[<Fact>]
let ``Basic test``() = task {
    
    let runner = createRunner 100 (fun i -> 10 + i |> decimal)

    let! results = 
        runner.Run(
            UserState(),
            100,
            10,
            Some 5m,
            TestDataGenerator.NET,
            DateTimeOffset.UtcNow,
            false
        )

    let oneThirdResult = results.Results[0]

    let maxDrawdown = oneThirdResult.MaxDrawdownPct
    let maxGain = oneThirdResult.MaxGainPct
    let position = oneThirdResult.Position |> StockPositionWithCalculations

    position.IsClosed |> should equal true
    position.Profit |> should equal 1005
    position.GainPct |> should equal 1.005m
    position.RR |> should equal 2.01m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 1.501m
    position.DaysHeld |> should equal 14

    let oneThirdPercentBased = results.Results[1]
    let maxDrawdown = oneThirdPercentBased.MaxDrawdownPct
    let maxGain = oneThirdPercentBased.MaxGainPct
    let position = oneThirdPercentBased.Position |> StockPositionWithCalculations

    position.IsClosed |> should equal true
    position.Profit |> should equal 137.3m
    position.GainPct |> should equal 0.1373m
    position.RR |> should equal 0.2746m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 0.201m
    position.DaysHeld |> should equal 1
}

[<Fact>]
let ``With portion size too small, still sells at RR levels``() =
    
    let bars = generatePriceBars 100 (fun i -> 10 + i |> decimal)

    let runner = TradingStrategyFactory.createProfitPointsTrade 3
    
    let result =
        StockPosition.openLong TestDataGenerator.NET DateTimeOffset.UtcNow
        |> StockPosition.buy 2m 10m DateTimeOffset.UtcNow None
        |> StockPosition.setStop (Some 5m) DateTimeOffset.UtcNow
        |> runner.Run bars
    
    let maxDrawdown = result.MaxDrawdownPct
    let maxGain = result.MaxGainPct
    let position = result.Position |> StockPositionWithCalculations
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 15
    position.GainPct |> should equal 0.75m
    position.RR |> should equal 1.5m
    maxDrawdown |> should equal -0.001m
    maxGain |> should equal 1.001m
    position.DaysHeld |> should equal 9

[<Fact>]
let ``With position not fully sold, is open``() = task {
    let runner = createRunner 100 (fun i -> 10 + i |> decimal)

    let! result = runner.Run(
        UserState(),
        numberOfShares = 2,
        price = 50,
        stopPrice = Some 0.01m,
        ticker = TestDataGenerator.NET,
        ``when``=DateTimeOffset.UtcNow,
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
            StockPosition.openLong TestDataGenerator.NET DateTimeOffset.UtcNow
            |> StockPosition.buy 5m 50m DateTimeOffset.UtcNow None
            |> StockPosition.setStop (Some 45m) DateTimeOffset.UtcNow
        
        (bars, positionInstance)
        
[<Fact>]
let ``With price falling, stop price exit executes``() =
    
    let bars, positionInstance = createDownsideTestData()
    let runner = TradingStrategyFactory.createProfitPointsTrade 3
    let result = runner.Run bars positionInstance
    
    let position = result.Position |> StockPositionWithCalculations
    let maxGain = result.MaxGainPct
    let maxDrawdown = result.MaxDrawdownPct
    
    position.IsClosed |> should equal true
    position.Profit |> should equal -25
    position.GainPct |> should equal -0.1m
    position.RR |> should equal -1m
    position.DaysHeld |> should equal 4
    maxGain |> should equal 0.0002m
    maxDrawdown |> should equal -0.1002m

[<Fact>]
let ``Close after fixed number of days, works``() =
    
    let data = generatePriceBars 10 (fun i -> 50 + i |> decimal)

    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET DateTimeOffset.UtcNow
        |> StockPosition.buy 5m 50m DateTimeOffset.UtcNow None
        |> StockPosition.setStop (Some 45m) DateTimeOffset.UtcNow
        
    let runner = TradingStrategyFactory.createCloseAfterFixedNumberOfDays 5
    
    let result = runner.Run data positionInstance
    
    let position = result.Position |> StockPositionWithCalculations
    let maxGain = result.MaxGainPct
    let maxDrawdown = result.MaxDrawdownPct
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 30
    position.GainPct |> should equal 0.12m
    position.RR |> should equal 1.2m
    position.DaysHeld |> should equal 5
    maxGain |> should equal 0.1202m
    maxDrawdown |> should equal -0.0002m