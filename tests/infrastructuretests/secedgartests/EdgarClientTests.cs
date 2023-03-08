using secedgar;

namespace secedgartests;

public class EdgarClientTests
{
    [Fact]
    public async Task TestAsync()
    {
        var client = new EdgarClient("NGTD/1.0");
        var filings = await client.GetCompanyFilingsAsync("AAPL");
        Assert.True(filings.Count > 0);
    }
}