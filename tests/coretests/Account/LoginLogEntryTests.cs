using System;
using core.Account;
using Xunit;

namespace coretests.Account
{
    public class LoginLogEntryTests
    {
        [Fact]
        public void CstrWorks()
        {
            var date = DateTime.UtcNow.AddDays(-1);

            var entry = new LoginLogEntry("laimonas", date);

            Assert.Equal("laimonas", entry.Username);
            Assert.Equal(date, entry.Date);
        }
    }
}