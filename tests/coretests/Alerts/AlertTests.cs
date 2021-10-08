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

            _uat.AddPricePoint("initial", 50);
            _uat.AddPricePoint("updated", 50); // the same price point twice
            _uat.AddPricePoint("10% up", 50m + 0.1m*50);
            _uat.AddPricePoint("10% down", 50m - 0.1m*50);

            _uat.AddPricePoint("real high", 60);

            var last = _uat.PricePoints.Last().Id;

            _uat.RemovePricePoint(last);

            _uat.RemovePricePoint(Guid.NewGuid()); // make sure it does not blow up
        }

        [Fact]
        public void AlertCountMatches()
        {
            Assert.Equal(3, _uat.PricePoints.Count);
        }


        [Theory]
        [InlineData(45)]
        [InlineData(50)]
        [InlineData(55)]
        public void PricePointsMatch(decimal point)
        {
            Assert.Contains(point, _uat.PricePoints.Select(pp => pp.Value));
        }

        [Theory]
        [InlineData(60)]
        public void PricePointsRemoved(decimal point)
        {
            Assert.DoesNotContain(point, _uat.PricePoints.Select(pp => pp.Value));
        }

        [Fact]
        public void TickerMatches()
        {
            Assert.Equal("AMD", _uat.Ticker);
        }
    }
}