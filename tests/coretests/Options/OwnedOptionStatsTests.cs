using System;
using core.fs.Options;
using core.Options;
using coretests.testdata;
using Xunit;
using OptionType = core.Options.OptionType;

namespace coretests.Options
{
    public class OwnedOptionStatsTests
    {
        private readonly OwnedOptionStats _stats;

        public OwnedOptionStatsTests()
        {
            var userId = Guid.NewGuid();
            var soldOption1 = new OwnedOption(TestDataGenerator.TSLA, 45, OptionType.CALL, new DateTimeOffset(2019, 10, 10, 0, 0, 0, TimeSpan.Zero), userId);
            soldOption1.Sell(1, 10, DateTimeOffset.Parse("2019-10-10"), null);
            soldOption1.Expire(assign: true);
            var o1 = new OwnedOptionView(soldOption1.State, optionDetail: null);

            var soldOption2 = new OwnedOption(TestDataGenerator.TSLA, 45, OptionType.CALL, new DateTimeOffset(2019, 10, 10, 0, 0, 0, TimeSpan.Zero), userId);
            soldOption2.Sell(1, 150, DateTimeOffset.Parse("2019-10-10"), null);
            soldOption2.Buy(1, 100, DateTimeOffset.Parse("2019-10-10"), null);
            var o2 = new OwnedOptionView(soldOption2.State, optionDetail: null);

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
            Assert.Equal(2, _stats.Wins);
        }

        [Fact]
        public void AssignmentsMatch()
        {
            Assert.Equal(1, _stats.Assigned);
        }
    }
}
