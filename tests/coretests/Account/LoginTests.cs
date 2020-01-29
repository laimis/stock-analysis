using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using coretests.Fakes;
using Xunit;

namespace coretests.Account
{
    public class LoginTests
    {
        // TODO: comment back in once ready to implement loging in tracking
        // [Fact]
        // public async Task Login_RecordsEntryAsync()
        // {
        //     var cmd = CreateCommand();

        //     var storage = new FakeAccountStorage();

        //     var handler = new Login.Handler(storage);
        //     await handler.Handle(cmd, CancellationToken.None);
        // }

        [Fact]
        public void LoginCommand_SetsDate()
        {
            var cmd = CreateCommand();

            Assert.NotNull(cmd.Timestamp);
        }

        private static Login.Command CreateCommand()
        {
            return new Login.Command("id", "ip");
        }
    }
}