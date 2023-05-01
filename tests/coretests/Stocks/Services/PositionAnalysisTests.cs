using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;
using core.Stocks;
using core.Stocks.Services.Analysis;
using coretests.TestData;
using Xunit;

namespace coretests.Stocks.Services
{
    public class PositionAnalysisTests
    {
        private (PositionInstance position, PriceBar[] bars) CreateTestData()
        {
            var position = new PositionInstance(0, "SHEL");
            position.Buy(numberOfShares: 10, price: 100m, when: System.DateTimeOffset.UtcNow, transactionId: System.Guid.NewGuid());
            position.SetPrice(110m);
            
            var bars = TestDataGenerator.PriceBars("SHEL");

            return (position, bars);
        }
        
        [Fact]
        public void Generate_WithNoStrategy_SetsRightLabel()
        {
            var (position, bars) = CreateTestData();

            var outcomes = PositionAnalysis.Generate(position, bars);

            Assert.Contains(outcomes, o => o.key == PortfolioAnalysisKeys.StrategyLabel && o.value == 0);
        }

        [Fact]
        public void Evaluate_WithNoStrategy_SelectsTicker()
        {
            var (position, bars) = CreateTestData();

            var outcomes = new List<TickerOutcomes> {
                new TickerOutcomes(
                    PositionAnalysis.Generate(position, bars).ToList(),
                    ticker: position.Ticker
                )
            };

            var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(outcomes, filings: null);

            Assert.Contains(evaluations, e => e.sortColumn == PortfolioAnalysisKeys.StrategyLabel);
        }
    }
}