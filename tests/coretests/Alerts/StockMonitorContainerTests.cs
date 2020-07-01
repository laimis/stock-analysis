using System;
using System.Collections.Generic;
using System.Linq;
using core.Alerts;
using core.Shared;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorContainerTests
    {
        private StockMonitorContainer _uat;
        private List<StockMonitorTrigger> _initialTriggers;
        private List<StockMonitorTrigger> _subsequentTriggers;

        public StockMonitorContainerTests()
        {
            _uat = new StockMonitorContainer();

            var a1 = new Alert(new Ticker("AMD"), Guid.NewGuid());
            a1.AddPricePoint(50);

            var a2 = new Alert(new Ticker("BAC"), Guid.NewGuid());
            a2.AddPricePoint(20);

            _uat.Register(a1);
            _uat.Register(a2);
            _uat.Register(a2);

            _initialTriggers = _uat.UpdateValue("AMD", 50).ToList();
            _subsequentTriggers = _uat.UpdateValue("AMD", 49).ToList();
        }

        [Fact]
        public void TickersMatch()
        {
            Assert.Equal(2, _uat.GetTickers().Count());
        }

        [Fact]
        public void InitialUpdateNoTriggers()
        {
            Assert.Empty(_initialTriggers);
        }

        [Fact]
        public void SubsequentUpdateTriggers()
        {
            Assert.Single(_subsequentTriggers);
        }

        [Fact]
        public void SubsequentUpdateTriggersMatches()
        {
            var t = _subsequentTriggers[0];

            Assert.Equal(49, t.Price);
        }

    }
}