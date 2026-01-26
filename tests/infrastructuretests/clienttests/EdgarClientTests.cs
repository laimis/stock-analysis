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
        var client = new EdgarClient(null);
        var response = await client.GetFilings(new Ticker("TTD"));
        Assert.NotEmpty(response.ResultValue.Filings);
    }
}
