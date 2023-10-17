using System;
using core.Shared;
using Xunit;

namespace coretests.Shared
{
    public class TickerTests
    {
        [Fact]
        public void NullHandling()
        {
            string val = null;

            Assert.Throws<ArgumentException>( () => new Ticker(val));
        }
    }
}