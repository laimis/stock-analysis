using System.Collections.Generic;
using core.fs.Services;
using core.fs.Services.Analysis;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Stocks;
using core.Shared;
using core.Stocks;
using coretests.testdata;
using Microsoft.FSharp.Core;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PositionAnalysisTests
    {
        private static (PositionInstance position, PriceBar[] bars, Order[] orders) CreateTestData()
        {
            var position = new PositionInstance(0, TestDataGenerator.TSLA, System.DateTimeOffset.UtcNow);
            position.Buy(numberOfShares: 10, price: 100m, when: System.DateTimeOffset.UtcNow, transactionId: System.Guid.NewGuid());
            position.SetPrice(110m);
            
            var bars = TestDataGenerator.PriceBars(TestDataGenerator.TSLA.Value);

            var orders = new[] {
                new Order {
                    Ticker = new FSharpOption<Ticker>(TestDataGenerator.TSLA),
                    Price = 100m,
                    Type = "SELL"
                }
            };

            return (position, bars, orders);
        }
        
        [Fact]
        public void Generate_WithNoStrategy_SetsRightLabel()
        {
            var (position, bars, orders) = CreateTestData();

            var outcomes = PositionAnalysis.generate(position, bars, orders);

            Assert.Contains(outcomes, o => o.Key == PositionAnalysis.PortfolioAnalysisKeys.StrategyLabel && o.Value == 0);
        }

        [Fact]
        public void Evaluate_WithNoStrategy_SelectsTicker()
        {
            var (position, bars, orders) = CreateTestData();

            var outcomes = new List<TickerOutcomes> {
                new TickerOutcomes(
                    PositionAnalysis.generate(position, bars,orders),
                    ticker: position.Ticker
                )
            };

            var evaluations = PositionAnalysis.evaluate(outcomes);

            Assert.Contains(evaluations, e => e.SortColumn == PositionAnalysis.PortfolioAnalysisKeys.StrategyLabel);
        }
    }
}