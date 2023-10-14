using System.Collections.Generic;
using System.Linq;
using core.fs.Shared.Adapters.Brokerage;
using core.Shared;
using core.Shared.Adapters.Stocks;
using core.Stocks;
using core.Stocks.Services.Analysis;
using coretests.testdata;
using Microsoft.FSharp.Core;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PositionAnalysisTests
    {
        private static (PositionInstance position, PriceBar[] bars, Order[] orders) CreateTestData()
        {
            var position = new PositionInstance(0, "SHEL", System.DateTimeOffset.UtcNow);
            position.Buy(numberOfShares: 10, price: 100m, when: System.DateTimeOffset.UtcNow, transactionId: System.Guid.NewGuid());
            position.SetPrice(110m);
            
            var bars = TestDataGenerator.PriceBars("SHEL");

            var orders = new[] {
                new Order {
                    Ticker = new FSharpOption<Ticker>(new Ticker("SHEL")),
                    Price = 100m,
                    Type = "SELL"
                }
            };

            return (position, bars, orders);
        }
        
        [Fact]
        public void Generate_WithNoStrategy_SetsRightLabel()
        {
            var (position, bars, _) = CreateTestData();

            var outcomes = PositionAnalysis.Generate(position, bars);

            Assert.Contains(outcomes, o => o.key == PortfolioAnalysisKeys.StrategyLabel && o.value == 0);
        }

        [Fact]
        public void Evaluate_WithNoStrategy_SelectsTicker()
        {
            var (position, bars, _) = CreateTestData();

            var outcomes = new List<TickerOutcomes> {
                new TickerOutcomes(
                    PositionAnalysis.Generate(position, bars).ToList(),
                    ticker: position.Ticker
                )
            };

            var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(outcomes);

            Assert.Contains(evaluations, e => e.sortColumn == PortfolioAnalysisKeys.StrategyLabel);
        }
    }
}