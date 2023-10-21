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
            var ticker = new Ticker("SHEL");
            
            var bars = TestDataGenerator.PriceBars(ticker.Value);

            var position = new PositionInstance(0, ticker, bars[0].Date);
            position.Buy(numberOfShares: 10, price: 100m, when: bars[0].Date, transactionId: System.Guid.NewGuid());
            position.SetPrice(bars[0].Close);
            
            var orders = new[] {
                new Order {
                    Ticker = new FSharpOption<Ticker>(ticker),
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

        [Fact]
        public void DailyPL_Correct()
        {
            var (position, bars, orders) = CreateTestData();
            
            var midPointInBars = bars.Length / 2;
            
            position.Sell(
                numberOfShares: position.NumberOfShares,
                price: bars[midPointInBars].Close,
                when: bars[midPointInBars].Date,
                transactionId: System.Guid.NewGuid());

            var dailyPlAndGain= PositionAnalysis.dailyPLAndGain(bars, position);
            
            Assert.Equal(midPointInBars + 1, dailyPlAndGain.Item1.Data.Count);
            Assert.Equal(midPointInBars + 1, dailyPlAndGain.Item2.Data.Count);
        }
    }
}