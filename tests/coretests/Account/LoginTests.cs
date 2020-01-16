using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using coretests.Fakes;
using Xunit;

namespace coretests.Account
{
    public class LoginTests
    {
        [Fact]
        public async Task Login_RecordsEntryAsync()
        {
            var cmd = CreateCommand();

            var handler = new Login.Handler(new FakeAccountStorage());
            await handler.Handle(cmd, CancellationToken.None);
        }

        [Fact]
        public void LoginCommand_SetsDate()
        {
            var cmd = CreateCommand();

            Assert.NotEqual(DateTime.MinValue, cmd.Date);
        }

        private static Login.Command CreateCommand()
        {
            return new Login.Command("username");
        }
    }
}