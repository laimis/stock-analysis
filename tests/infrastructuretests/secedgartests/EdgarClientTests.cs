using core.Shared;
using secedgar;

namespace secedgartests;

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Fact]
    public async Task TestAsync()
    {
        var client = new EdgarClient(null, "NGTD/1.0");
        var response = await client.GetFilings(new Ticker("AAPL"));
        Assert.NotEmpty(response.Success.Value.Filings);
    }
}