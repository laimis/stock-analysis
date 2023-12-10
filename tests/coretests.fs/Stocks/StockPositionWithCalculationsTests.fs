module coretests.fs.Stocks.StockPositionWithCalculationsTests

open System
open FsUnit
open Xunit
open core.fs.Shared.Domain
open coretests.testdata


let position =
    StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23")) None
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25")) None
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25")) None
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21")) None
    |> StockPositionWithCalculations

[<Fact>]
let ``LastBuyPrice_Accurate`` () =
    position.LastBuyPrice |> should equal 35m

[<Fact>]
let ``LastSellPrice_Accurate`` () =
    position.LastSellPrice |> should equal 37m

[<Fact>]
let ``RR_Accurate`` () =
    let position =
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23")) None
        |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25")) None
        |> StockPosition.setStop (Some 28m) (DateTimeOffset.Parse("2020-01-25"))
        |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25")) None
        |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21")) None
        |> StockPositionWithCalculations
        
    Assert.Equal(1.33m, position.RR, 2);

// [<Fact>]
//
//         [Fact]
//         public void RR_Accurate() =>
//             Assert.Equal(3.69m, _position.RR, 2);
//
//         [Fact]
//         public void GainPct_Accurate() =>
//             Assert.Equal(0.185m, _position.GainPct, 2);
//
//         [Fact]
//         public void FirstBuyCost_Accurate() =>
//             Assert.Equal(32.5m, _position.CompletedPositionCostPerShare);
//
//         [Fact]
//         public void FirstBuyNumberOfShares_Accurate() =>
//             Assert.Equal(20, _position.CompletedPositionShares);
//
//         [Fact]
//         public void FirstStop_Accurate() =>
//             Assert.Equal(30.875m, _position.FirstStop);
//
//         [Fact]
//         public void RiskedAmount_Accurate() =>
//             Assert.Equal(32.5m, _position.RiskedAmount);
//
//         [Fact]
//         public void AverageCost_Accurate() =>
//             Assert.Equal(32.5m, _position.AverageBuyCostPerShare);
//
//         [Fact]
//         public void DaysHeld() =>
//             Assert.True(Math.Abs(57 - _position.DaysHeld) <= 1);
//
//         [Fact]
//         public void StopPriceGetsSetAfterSell() =>
//             Assert.Equal(30.875m, _position.StopPrice);
//
//         [Fact]
//         public void Cost()
//         {
//             var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));
//
//             position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
//             position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());
//
//             Assert.Equal(650, position.Cost);
//         }
//
//         [Fact]
//         public void SetStop_SetsFirstStop()
//         {
//             var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));
//
//             position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
//             position.SetStopPrice(28, DateTimeOffset.UtcNow);
//             position.SetStopPrice(29, DateTimeOffset.UtcNow);
//             Assert.Equal(28, position.FirstStop);
//         }
//
//         [Fact]
//         public void Profit() => Assert.Equal(120, _position.Profit);
//
//         [Fact]
//         public void IsClosed() => Assert.True(_position.IsClosed);
//
//         [Fact]
//         public void Ticker() => Assert.Equal(TestDataGenerator.TSLA, _position.Ticker);
//
//         [Fact]
//         public void SetRisk_Zero_Ignores()
//         {
//             var original = _position.RiskedAmount!.Value;
//             
//             _position.SetRiskAmount(0, DateTimeOffset.UtcNow);
//             
//             Assert.Equal(original, _position.RiskedAmount);
//         }
//     }
// }