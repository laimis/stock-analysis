using System;
using core.Shared;
using Xunit;

namespace coretests.Utils
{
    public class NotInThePastTests
    {
        [Fact]
        public void InThePast_Fails()
        {
            var att = new NotInThePast();

            Assert.False(att.IsValid(DateTimeOffset.UtcNow.AddDays(-1)));
        }

        [Fact]
        public void InFuture_Succeeds()
        {
            var att = new NotInThePast();

            Assert.True(att.IsValid(DateTimeOffset.UtcNow.AddDays(1)));
        }

        [Fact]
        public void Message_Formatted()
        {
            var att = new NotInThePast();

            var formatted = att.FormatErrorMessage("myparam");

            Assert.Contains("myparam", formatted);
            Assert.Contains("past", formatted);
        }
    }
}