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
            Assert.Throws<InvalidOperationException>(() => new AggregateEvent(Guid.Empty, Guid.NewGuid(), DateTimeOffset.UtcNow));
        }

        [Fact]
        public void CreateWithBadUserIdFails()
        {
            Assert.Throws<InvalidOperationException>(() => new AggregateEvent(Guid.NewGuid(), Guid.Empty, DateTimeOffset.UtcNow));
        }
    }
}