using System.Threading.Tasks;
using core.Account;
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
            var client = new TDAmeritradeClient(
                null,
                config[0],
                config[1]
            );

            var user = new User("test", "test", "test");
            user.ConnectToBrokerage(
                config[2],
                config[3],
                "type",
                1,
                "name",
                1
            );

            var positions = await client.GetPositions(user.State);

            Assert.NotEmpty(positions);
        }
    }
}