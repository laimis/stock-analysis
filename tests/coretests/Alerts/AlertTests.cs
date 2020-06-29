using System;
using System.Linq;
using core.Alerts;
using core.Shared;
using Xunit;

namespace coretests.Alerts
{
    public class AlertTests
    {
        private Alert _uat;

        public AlertTests()
        {
            _uat = new Alert(new Ticker("AMD"), Guid.NewGuid());

            _uat.AddPricePoint(50);
            _uat.AddPricePoint(50 + 0.1*50);
            _uat.AddPricePoint(50 - 0.1*50);
        }

        [Theory]
        [InlineData(45)]
        [InlineData(50)]
        [InlineData(55)]
        public void PricePointsMatch(double point)
        {
            Assert.Contains(point, _uat.PricePoints.Select(pp => pp.Value));
        }

        [Fact]
        public void TickerMatches()
        {
            Assert.Equal("AMD", _uat.Ticker);
        }
    }
}