using System.Threading.Tasks;
using core.fs.Adapters.Brokerage;
using core.fs.Accounts;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Moq;
using tdameritradeclient;
using Xunit;

namespace tdameritradeclienttests
{

    public class TDAmeritradeSerializationTests
    {
        [Fact]
        public void NaNSerialization_Works()
        {
            var jsonWithNan = @"{""volatility"": ""NaN""}";
            var jsonWithoutNan = @"{""volatility"": 1.0}";

            var deserializedWithNan = System.Text.Json.JsonSerializer.Deserialize<OptionDescriptor>(jsonWithNan);

            Assert.Equal(0, deserializedWithNan!.volatility);

            var deserializedWithoutNan = System.Text.Json.JsonSerializer.Deserialize<OptionDescriptor>(jsonWithoutNan);
            
            Assert.Equal(1.0m, deserializedWithoutNan!.volatility);
        }
    }

    // [Trait("Category", "Integration")]
    // public class TDAmeritradeClientTests
    // {
    //     [Fact]
    //     public async Task PriceHistoryTest()
    //     {
    //         var config = testutils.CredsHelper.GetTDAmeritradeConfig().Split(',');
    //         var tokenJson = testutils.CredsHelper.GetTDAmeritradeToken();
    //         var at = System.Text.Json.JsonSerializer.Deserialize<OAuthResponse>(tokenJson);
    //         var client = new TDAmeritradeClient(
    //             new MemoryAggregateStorage(Mock.Of<IOutbox>()),
    //             config[0],
    //             config[1],
    //             null
    //         );
    //
    //         var user = User.Create("test", "test", "test");
    //         user.ConnectToBrokerage(
    //             at!.access_token,
    //             at!.refresh_token,
    //             "type",
    //             1,
    //             "name",
    //             1
    //         );
    //
    //         var chainResponse = await client.GetOptions(user.State, new Ticker("RELL"), FSharpOption<DateTimeOffset>.None, FSharpOption<decimal>.None, FSharpOption<string>.None);
    //
    //         var chain = chainResponse.Success!;
    //
    //         Assert.NotNull(chain);
    //     }
    // }
}
