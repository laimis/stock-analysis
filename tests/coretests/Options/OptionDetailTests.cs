using System;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class OptionDetailTests
    {
        private OptionDetail _put;
        private OptionDetail _call;

        public OptionDetailTests()
        {
            _put = new OptionDetail();

            _put.Side = "put";
            _put.Bid = 1;
            _put.Ask = 2;
            _put.ExpirationDate = DateTime.UtcNow.AddDays(20).ToString("yyyyMMdd");
            _put.StrikePrice = 22;
            _put.OpenInterest = 1;
            _put.Volume = 2;

            _call = new OptionDetail();

            _call.Side = "call";
            _call.Bid = 1;
            _call.Ask = 2;
            _call.ExpirationDate = DateTime.UtcNow.AddDays(20).ToString("yyyyMMdd");
            _call.StrikePrice = 22;
            _call.OpenInterest = 1;
            _call.Volume = 2;
        }

        [Fact]
        public void CallChecks()
        {
            Assert.True(_call.IsCall);
            Assert.False(_call.IsPut);
            Assert.Equal(23, _call.BreakEven);
            Assert.Equal(0.045, _put.Risk, 3);
            Assert.Equal(5, _call.PerDayPrice);
            Assert.Equal(1, _call.Spread);
            Assert.Equal(1, _call.OpenInterest);
            Assert.Equal(2, _call.Volume);
            Assert.Equal(_call.OptionType, _call.Side);
        }

        [Fact]
        public void PutChecks()
        {
            Assert.False(_put.IsCall);
            Assert.True(_put.IsPut);
            Assert.Equal(21, _put.BreakEven);
            Assert.Equal(0.045, _put.Risk, 3);
            Assert.Equal(5, _put.PerDayPrice);
            Assert.Equal(1, _put.Spread);
            Assert.Equal(1, _put.OpenInterest);
            Assert.Equal(2, _put.Volume);
            Assert.Equal(_put.OptionType, _put.Side);
        }
    }
}