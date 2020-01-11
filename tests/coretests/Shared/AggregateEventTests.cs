using System;
using core.Shared;
using Xunit;

namespace coretests.Shared
{
    public class AggregateEventTests
    {
        [Fact]
        public void CreateWithBadKeyFails()
        {
            Assert.Throws<InvalidOperationException>(() => new AggregateEvent(null, "userid", DateTime.UtcNow));
        }

        [Fact]
        public void CreateWithBadUserIdFails()
        {
            Assert.Throws<InvalidOperationException>(() => new AggregateEvent("key", null, DateTime.UtcNow));
        }
    }
}