using System;
using core.fs.Adapters.Options;
using core.fs.Options;
using Xunit;

namespace coretests.Adapters.Options
{
    public class OptionDetailTests
    {
        private OptionDetail _put = new(symbol: "TICKER", side: "put", description: "desc")
        {
            Bid = 1,
            Ask = 2,
            ParsedExpirationDate = DateTimeOffset.UtcNow.AddDays(20),
            StrikePrice = 22,
            OpenInterest = 1,
            Volume = 2
        };
        private OptionDetail _call = new(symbol: "TICKER", side: "call", description: "desc")
        {
            Bid = 1,
            Ask = 2,
            ParsedExpirationDate = DateTimeOffset.UtcNow.AddDays(20),
            StrikePrice = 22,
            OpenInterest = 1,
            Volume = 2
        };

        [Fact]
        public void CallChecks()
        {
            Assert.True(_call.IsCall);
            Assert.False(_call.IsPut);
            Assert.Equal(23, _call.BreakEven);
            Assert.Equal(0.045m, _put.Risk, 3);
            Assert.Equal(5, _call.PerDayPrice);
            Assert.Equal(1, _call.Spread);
            Assert.Equal(1, _call.OpenInterest);
            Assert.Equal(2, _call.Volume);
            Assert.Equal(_call.OptionType, OptionType.Call);
        }

        [Fact]
        public void PutChecks()
        {
            Assert.False(_put.IsCall);
            Assert.True(_put.IsPut);
            Assert.Equal(21, _put.BreakEven);
            Assert.Equal(0.045m, _put.Risk, 3);
            Assert.Equal(5, _put.PerDayPrice);
            Assert.Equal(1, _put.Spread);
            Assert.Equal(1, _put.OpenInterest);
            Assert.Equal(2, _put.Volume);
            Assert.Equal(_put.OptionType, OptionType.Put);
        }
    }
}
