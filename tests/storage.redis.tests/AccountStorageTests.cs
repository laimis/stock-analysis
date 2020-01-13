using System;
using System.Threading.Tasks;
using core.Account;
using Xunit;

namespace storage.redis.tests
{
    [Trait("Category", "Database")]
    public class AccountStorageTests
    {
        [Fact]
        public async Task EndToEndAsync()
        {
            var storage = new AccountStorage("localhost");

            await storage.RecordLoginAsync(new LoginLogEntry("laimonas", DateTime.UtcNow));

            var list = await storage.GetLogins();

            Assert.NotEmpty(list);
        }
    }
}
