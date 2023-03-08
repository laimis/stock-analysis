using secedgar;

namespace secedgartests;

[Trait("Category", "Integration")]
public class EdgarClientTests
{
    [Fact]
    public async Task TestAsync()
    {
        var client = new EdgarClient("NGTD/1.0");
        var filings = await client.GetFilings("AAPL");
        Assert.True(filings.filings.Count > 0);
    }
}