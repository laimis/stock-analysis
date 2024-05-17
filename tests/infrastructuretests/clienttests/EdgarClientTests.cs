using System.Threading.Tasks;
using core.Shared;
using secedgar;
using Xunit;

namespace secedgartests;

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Fact]
    public async Task TestAsync()
    {
        var client = new EdgarClient(null, "NGTD/1.0");
        var response = await client.GetFilings(new Ticker("AAPL"));
        Assert.NotEmpty(response.ResultValue.Filings);
    }
}
