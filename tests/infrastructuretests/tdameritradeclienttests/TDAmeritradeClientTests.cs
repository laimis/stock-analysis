using System;
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
        public async Task PriceHistoryTest()
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

            var ordersResponse = await client.GetPriceHistory(user.State, "AAPL");

            var orders = ordersResponse.Success!;

            Assert.NotEmpty(orders);
            Assert.True(orders[0].Close > 0);
            Assert.True(orders[0].High > 0);
            Assert.True(orders[0].Low > 0);
            Assert.True(orders[0].Open > 0);
            Assert.True(orders[0].Volume > 0);
            Assert.Equal(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), orders[orders.Length - 1].DateStr);
        }
    }
}