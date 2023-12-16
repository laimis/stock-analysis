using core.fs.Adapters.Brokerage;

namespace timezonesupporttests;
public class MarketHoursTests
{
    private IMarketHours _marketHours;

    public MarketHoursTests()
    {
        _marketHours = new timezonesupport.MarketHours();
    }

    [Theory]
    [InlineData("2020-06-29T14:45:00Z", true)]
    [InlineData("2020-06-29T13:40:00Z", true)]
    [InlineData("2020-06-29T12:40:00Z", false)]
    public void IsOn(string time, bool isActiveMarket)
    {
        Assert.Equal(
            isActiveMarket,
            _marketHours.IsMarketOpen(DateTimeOffset.Parse(time))
        );
    }

}