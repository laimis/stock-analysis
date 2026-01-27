using System.Threading.Tasks;
using core.Shared;
using secedgar;
using Xunit;

namespace secedgartests;

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Theory]
    // [InlineData("AAPL")]
    // [InlineData("MSFT")]
    [InlineData("DOCN")]
    public async Task TestAsync(string ticker)
    {
        var client = new EdgarClient(null);
        var response = await client.GetFilings(new Ticker(ticker));

        // make sure it is not an error
        Assert.True(response.IsOk);
        Assert.NotEmpty(response.ResultValue.Filings);
    }
}
