module coretests.fs.Stocks.Services.Trading.TradingStrategyRunnerTests

open System
open System.Threading.Tasks
open Castle.Core.Logging
open Moq
open Xunit
open core.Account
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Services.Trading
open core.fs.Stocks
open testutils
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
                    prices |> Ok |> Task.FromResult
        }
    
    TradingStrategyRunner(mock, MarketHoursAlwaysOn(), Mock.Of<core.fs.Adapters.Logging.ILogger>()), prices

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

    let testResult = results.Results |> Seq.find (fun r -> r.StrategyName = "3 profit points at 0.07% intervals")

    let maxDrawdown = testResult.MaxDrawdownPct
    let maxGain = testResult.MaxGainPct
    let position = testResult.Position

    position.IsClosed |> should equal true
    position.Profit |> should equal 137.3m
    position.GainPct |> should equal 0.1373m
    position.RR |> should equal 0.2746m
    maxDrawdown |> should equal 0m
    maxGain |> should equal 0.2m
    position.DaysHeld |> should equal 2

    let oneThirdPercentBased = results.Results |> Seq.find (fun r -> r.StrategyName = "3 profit points at 0.10% intervals")
    let maxDrawdown = oneThirdPercentBased.MaxDrawdownPct
    let maxGain = oneThirdPercentBased.MaxGainPct
    let position = oneThirdPercentBased.Position

    position.IsClosed |> should equal true
    position.Profit |> should equal 167m
    position.GainPct |> should equal 0.167m
    position.RR |> should equal 0.334m
    maxDrawdown |> should equal 0m
    maxGain |> should equal 0.2m
    position.DaysHeld |> should equal 2
}

[<Fact>]
let ``With portion size too small, still sells at RR levels``() =
    
    let bars = generatePriceBars 100 (fun i -> 10 + i |> decimal)

    let runner = TradingStrategyFactory.createProfitPointsTrade 3
    
    let result =
        StockPosition.openLong TestDataGenerator.NET bars.First.Date
        |> StockPosition.buy 2m 10m  bars.First.Date
        |> StockPosition.setStop (Some 5m)  bars.First.Date
        |> runner.Run bars false
    
    let maxDrawdown = result.MaxDrawdownPct
    let maxGain = result.MaxGainPct
    let position = result.Position
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 15
    position.GainPct |> should equal 0.75m
    position.RR |> should equal 1.5m
    maxDrawdown |> should equal 0m
    maxGain |> should equal 1m
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
    |> Seq.filter (fun r -> r.StrategyName = "Close after 120 days")
    |> Seq.iter (fun r ->
        r.Position.IsClosed |> should equal false
    )
}
        
[<Fact>]
let ``With price falling, stop price exit executes``() =
    
    let bars = generatePriceBars 10 (fun i -> 50 - i |> decimal)
    
    let positionInstance = 
        StockPosition.openLong TestDataGenerator.NET bars.First.Date
        |> StockPosition.buy 5m 50m bars.First.Date
        |> StockPosition.setStop (Some 45m) bars.First.Date
    
    let strategy = TradingStrategyFactory.createProfitPointsTrade 3
    let result = strategy.Run bars false positionInstance
    
    let position = result.Position
    let maxGain = result.MaxGainPct
    let maxDrawdown = result.MaxDrawdownPct
    
    position.IsClosed |> should equal true
    position.Profit |> should equal -25
    position.GainPct |> should equal -0.1m
    position.RR |> should equal -1m
    position.DaysHeld |> should equal 5
    maxGain |> should equal 0m
    maxDrawdown |> should equal -0.1m
    
[<Fact>]
let ``With price falling, short position simulates profit taking``() =
    
    let bars = generatePriceBars 10 (fun i -> 100 - i*5 |> decimal)
    
    let positionInstance = 
        StockPosition.openShort TestDataGenerator.NET bars.First.Date
        |> StockPosition.sell 5m 100m bars.First.Date
        |> StockPosition.setStop (Some 105m) bars.First.Date
        
    let strategy = TradingStrategyFactory.createProfitPointsTrade 3
    
    let result = strategy.Run bars false positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal true
    position.Profit |> should equal 60
    Math.Round(position.GainPct, 2) |> should equal 0.14m
    position.RR |> should equal 2.4m
    position.DaysHeld |> should equal 3
    
    // mae and mfe are correctly calculated for shorts
    result.MaxDrawdownPct |> should equal 0m
    result.MaxGainPct |> should equal 0.15m
    

[<Fact>]
let ``Close after fixed number of days, works``() =
    
    let data = generatePriceBars 10 (fun i -> 50 + i |> decimal)

    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date
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
    maxGain |> should equal 0.12m
    maxDrawdown |> should equal -0m
    
[<Fact>]
let ``Trailing stop adjusts as the long position gains``() =
    
    let data = generatePriceBars 10 (fun i -> 50 + i |> decimal)
    
    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date
        |> StockPosition.setStop (Some 45m) data.First.Date
        
    let runner = TradingStrategyFactory.createTrailingStop "test trailing stop" 0.1m None
    
    let result = runner.Run data false positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal false
    result.MaxGainPct |> should equal 0.2m
    result.MaxDrawdownPct |> should equal 0m
    position.StopPrice |> Option.get |> should equal 54m
    
[<Fact>]
let ``Trailing stop with initial stop starts with initial stop``() =
    
    let data = generatePriceBars 10 (fun i -> 50 + i |> decimal)
    
    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date
        |> StockPosition.setStop (Some 45m) data.First.Date
        
    let runner = TradingStrategyFactory.createTrailingStop "test trailing stop" 0.5m (Some 50m)
    
    let result = runner.Run data false positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal false
    result.MaxGainPct |> should equal 0.2m
    result.MaxDrawdownPct |> should equal 0m
    // the trailing stop should continuously be lower than the initial stop, so it should not come into play
    // and initial stop should remain until trailing stop is higher
    position.StopPrice |> Option.get |> should equal 50m
    
[<Fact>]
let ``Trailing stop adjusts as the short position drops`` () =
    
    let data = generatePriceBars 10 (fun i -> 100 - i*5 |> decimal)
    
    let positionInstance =
        StockPosition.openShort TestDataGenerator.NET data.First.Date
        |> StockPosition.sell 5m 100m data.First.Date
        |> StockPosition.setStop (Some 105m) data.First.Date
        
    let runner = TradingStrategyFactory.createTrailingStop "test trailing stop" 0.1m None
    
    let result = runner.Run data false positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal false
    result.MaxGainPct |> should equal 0.5m
    result.MaxDrawdownPct |> should equal 0m
    position.StopPrice |> Option.get |> should equal 55m
    
[<Fact>]
let ``Fixed number of days but with stop for a long position that's falling, respects stop`` () =
    let data = generatePriceBars 10 (fun i -> 50 - i |> decimal)
    
    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date
        |> StockPosition.setStop (Some 45m) data.First.Date
        
    let runner = TradingStrategyFactory.createCloseAfterFixedNumberOfDaysWithStop "desc" 30 45m
    
    let result = runner.Run data false positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal true
    position.DaysHeld |> should equal 6
    position.Profit |> should equal -30
    position.GainPct |> should equal -0.12m
    position.RR |> should equal -1.2m
    result.MaxGainPct |> should equal 0m
    result.MaxDrawdownPct |> should equal -0.12m
    


[<Fact>]
let ``Buy and Hold holds until the end of bars`` () =
    let data = generatePriceBars 10 (fun i -> 50 - i |> decimal)
    
    let positionInstance =
        StockPosition.openLong TestDataGenerator.NET data.First.Date
        |> StockPosition.buy 5m 50m data.First.Date
        |> StockPosition.setStop (Some 45m) data.First.Date
        
    let runner = TradingStrategyFactory.createBuyAndHold
    
    let result = runner.Run data true positionInstance
    
    let position = result.Position
    
    position.IsClosed |> should equal true
    result.ForcedClosed |> should equal true
    position.DaysHeld |> should equal 10
    position.Profit |> should equal -50m
    position.GainPct |> should equal -0.2m
    position.RR |> should equal -2m
    result.MaxGainPct |> should equal 0m
    result.MaxDrawdownPct |> should equal -0.2m
