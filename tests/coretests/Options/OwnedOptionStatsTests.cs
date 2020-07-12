using System;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class OwnedOptionStatsTests
    {
        private OwnedOptionStats _stats;

        public OwnedOptionStatsTests()
        {
            var o1 = new OwnedOptionSummary {
                Assigned = true,
                BoughtOrSold = "SOLD",
                Days = 10,
                DaysHeld = 9,
                ExpirationDate = "2019-10-10",
                ExpiresSoon = false,
                Filled = DateTimeOffset.Parse("2019-10-10"),
                Id = Guid.NewGuid(),
                IsExpired = true,
                NumberOfContracts = 1,
                OptionType = OptionType.CALL.ToString(),
                PremiumPaid = 10,
                PremiumReceived = 10,
                StrikePrice = 45,
                Ticker = "AMD",
                Transactions = null
            };

            var o2 = new OwnedOptionSummary {
                Assigned = false,
                BoughtOrSold = "SOLD",
                Days = 20,
                DaysHeld = 10,
                ExpirationDate = "2019-10-10",
                ExpiresSoon = false,
                Filled = DateTimeOffset.Parse("2019-10-10"),
                Id = Guid.NewGuid(),
                IsExpired = true,
                NumberOfContracts = 1,
                OptionType = OptionType.CALL.ToString(),
                PremiumPaid = 100,
                PremiumReceived = 150,
                StrikePrice = 45,
                Ticker = "AMD",
                Transactions = null
            };

            _stats = new OwnedOptionStats(new [] {o1, o2});
        }

        [Fact]
        public void CountMatches()
        {
            Assert.Equal(2, _stats.Count);
        }

        [Fact]
        public void WinningMatches()
        {
            Assert.Equal(1, _stats.WinningTrades);
        }

        [Fact]
        public void AssignmentsMatch()
        {
            Assert.Equal(1, _stats.Assigned);
        }
    }
}
