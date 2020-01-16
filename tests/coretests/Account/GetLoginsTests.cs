using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using coretests.Fakes;
using Xunit;

namespace coretests.Account
{
    public class GetLoginTests
    {
        [Fact]
        public async Task Login_RecordsEntryAsync()
        {
            var storage = new FakeAccountStorage();

            storage.Register(new LoginLogEntry("username", DateTime.UtcNow));

            var handler = new GetLogins.Handler(storage);
            
            var query = new GetLogins.Query();

            var list = await handler.Handle(query, CancellationToken.None);

            Assert.Single(list);
        }
    }
}