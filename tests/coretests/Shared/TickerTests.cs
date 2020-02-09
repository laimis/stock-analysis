using System;
using core.Shared;
using Xunit;

namespace coretests.Shared
{
    public class TickerTests
    {
        [Fact]
        public void ImplicitConversions()
        {
            string val = "tlsa";

            Ticker t = val;

            Assert.Equal("TLSA", t);
        }

        [Fact]
        public void NullHandling()
        {
            string val = null;

            Assert.Throws<ArgumentException>( () => {Ticker? t = val; });
        }
    }
}