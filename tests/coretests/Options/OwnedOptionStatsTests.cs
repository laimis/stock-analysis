using System;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class OwnedOptionStatsTests
    {
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
                PremiumReceived = 40,
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
                PremiumPaid = 150,
                PremiumReceived = 100,
                StrikePrice = 45,
                Ticker = "AMD",
                Transactions = null
            };

            var stats = new OwnedOptionStats(new [] {o1, o2});

            Assert.Equal(2, stats.Count);
            Assert.Equal(1, stats.WinningTrades);
            Assert.Equal(1, stats.Assigned);
        }
    }
}
