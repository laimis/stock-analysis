using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks;
using core.Stocks.Services.Analysis;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PositionAnalysisTests
    {
        private static (PositionInstance position, PriceBar[] bars, Order[] orders) CreateTestData()
        {
            var position = new PositionInstance(0, "SHEL");
            position.Buy(numberOfShares: 10, price: 100m, when: System.DateTimeOffset.UtcNow, transactionId: System.Guid.NewGuid());
            position.SetPrice(110m);
            
            var bars = TestDataGenerator.PriceBars("SHEL");

            var orders = new Order[] {
                new Order {
                    Ticker = "SHEL",
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

            var outcomes = PositionAnalysis.Generate(position, bars, orders);

            Assert.Contains(outcomes, o => o.key == PortfolioAnalysisKeys.StrategyLabel && o.value == 0);
        }

        [Fact]
        public void Evaluate_WithNoStrategy_SelectsTicker()
        {
            var (position, bars, orders) = CreateTestData();

            var outcomes = new List<TickerOutcomes> {
                new TickerOutcomes(
                    PositionAnalysis.Generate(position, bars, orders).ToList(),
                    ticker: position.Ticker
                )
            };

            var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(outcomes);

            Assert.Contains(evaluations, e => e.sortColumn == PortfolioAnalysisKeys.StrategyLabel);
        }
    }
}