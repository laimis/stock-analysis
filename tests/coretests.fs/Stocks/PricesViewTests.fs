module coretests.fs.Stocks.PricesViewTests

open Xunit
open core.fs.Adapters.Stocks
open core.fs.Stocks

[<Fact>]
let ``Givens prices SMAs are correct``() =
    
    // generate a set of 504 prices from 0 to 504 increasing by one each day
    let prices = 
        let baseDate = System.DateTimeOffset(2020, 1, 1, 0, 0, 0, System.TimeSpan.Zero)
        [|
            for i in 0 .. 503 do
                yield PriceBar(
                    date = baseDate.AddDays(float i),
                    ``open`` = decimal (i + 1),
                    high = decimal (i + 1),
                    low = decimal  (i + 1),
                    close = decimal (i + 1),
                    volume = 1000)
        |]

    let view = PricesView(PriceBars(prices, Daily))

    Assert.Equal(504, view.Prices.Length)
    Assert.Equal(5, view.MovingAverages.Length)
    Assert.Equal(20, view.MovingAverages.ema20.Interval)
    Assert.Equal(20, view.MovingAverages.sma20.Interval)
    Assert.Equal(50, view.MovingAverages.sma50.Interval)
    Assert.Equal(150, view.MovingAverages.sma150.Interval)
    Assert.Equal(200, view.MovingAverages.sma200.Interval)
    Assert.Equal(504, view.MovingAverages.ema20.Values.Length)
    Assert.Equal(504, view.MovingAverages.sma20.Values.Length)
    Assert.Equal(504, view.MovingAverages.sma50.Values.Length)
    Assert.Equal(504, view.MovingAverages.sma150.Values.Length)
    Assert.Equal(504, view.MovingAverages.sma200.Values.Length)
    Assert.Equal(494.5m, view.MovingAverages.ema20.LastValue.Value)
    Assert.Equal(493.5m, view.MovingAverages.sma20.LastValue.Value)
    Assert.Equal(478.5m, view.MovingAverages.sma50.LastValue.Value)
    Assert.Equal(428.5m, view.MovingAverages.sma150.LastValue.Value)
    Assert.Equal(403.5m, view.MovingAverages.sma200.LastValue.Value)
