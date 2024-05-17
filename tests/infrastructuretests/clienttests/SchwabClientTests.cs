using System.Threading.Tasks;
using core.fs.Adapters.Brokerage;
using core.fs.Accounts;
using core.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;
using Moq;
using Xunit;

namespace clienttests
{
    [Trait("Category", "Integration")]
    public class SchwabClientTests
    {
        [Fact(Skip = "Run only when testing and have token available")]
        public async Task Schwab()
        {
            //          var config = testutils.CredsHelper.GetSchwabConfig().Split(',');
            //         var tokenJson = testutils.CredsHelper.GetSchwabToken();
            //         var at = System.Text.Json.JsonSerializer.Deserialize<OAuthResponse>(tokenJson);
            
            var user = User.Create("test", "test", "test");
            user.ConnectToBrokerage("access", "refresh", "type", 1000000, "name");
            
            var schwab = new SchwabClient.SchwabClient(
                Mock.Of<IBlobStorage>(),
                "client",
                "secret",
                "redirect",
                new FSharpOption<ILogger<SchwabClient.SchwabClient>>(Mock.Of<ILogger<SchwabClient.SchwabClient>>())
            ) as IBrokerage;
            
            var quote = await schwab.Search(user.State, "NET", 15);
        
            var errorMessage = quote.IsError ? quote.ErrorValue.Message : null;
            Assert.Null(errorMessage);
            
            Assert.NotEmpty(quote.ResultValue);
            Assert.True(quote.ResultValue.Length > 1);
        }
    }
}
