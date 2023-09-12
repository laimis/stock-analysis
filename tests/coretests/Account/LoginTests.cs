using System;
using core.Account;
using core.Account.Handlers;
using Xunit;

namespace coretests.Account
{
    public class LoginTests
    {
        [Fact]
        public void LoginCommand_SetsDate()
        {
            var cmd = CreateCommand();

            Assert.Equal(
                DateTimeOffset.UtcNow.DateTime,
                cmd.Timestamp.DateTime,
                precision: TimeSpan.FromSeconds(1));
        }

        private static Login.Command CreateCommand()
        {
            return new Login.Command(Guid.NewGuid(), "ip");
        }
    }
}