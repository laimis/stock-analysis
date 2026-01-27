using System.Threading.Tasks;
using core.fs.Adapters.SEC;
using core.Shared;
using Microsoft.FSharp.Core;
using Xunit;

namespace secedgartests;

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Theory]
    [InlineData("AAPL")]
    [InlineData("MSFT")]
    [InlineData("DOCN")]
    public async Task TestAsync(string ticker)
    {
        var client = new secedgar.fs.EdgarClient(
            FSharpOption<Microsoft.Extensions.Logging.ILogger<secedgar.fs.EdgarClient>>.None
        ) as ISECFilings;
        
        var response = await client.GetFilings(new Ticker(ticker));

        // make sure it is not an error
        Assert.True(response.IsOk);
        Assert.NotEmpty(response.ResultValue.Filings);
    }
}
