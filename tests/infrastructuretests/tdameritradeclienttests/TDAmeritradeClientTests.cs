using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Brokerage;
using tdameritradeclient;
using Xunit;

namespace tdameritradeclienttests
{
    [Trait("Category", "Integration")]
    public class TDAmeritradeClientTests
    {
        [Fact]
        public async Task AccountTest()
        {
            var config = testutils.CredsHelper.GetTDAmeritradeConfig().Split(',');
            var tokenJson = testutils.CredsHelper.GetTDAmeritradeToken();
            var at = System.Text.Json.JsonSerializer.Deserialize<OAuthResponse>(tokenJson);
            var client = new TDAmeritradeClient(
                null,
                config[0],
                config[1]
            );

            var user = new User("test", "test", "test");
            user.ConnectToBrokerage(
                at!.access_token,
                at!.refresh_token,
                "type",
                1,
                "name",
                1
            );

            var orders = await client.GetOrders(user.State);

            Assert.NotEmpty(orders);
        }
    }
}