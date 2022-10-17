using System;
using System.Collections.Generic;
using System.Linq;
using core.Alerts;
using core.Shared;
using core.Stocks;
using Xunit;

namespace coretests.Alerts
{
    public class StockMonitorContainerTests
    {
        private StockMonitorContainer _uat;
        private List<TriggeredAlert> _initialTriggers;
        private List<TriggeredAlert> _subsequentTriggers;
        private OwnedStock _amd;
        private OwnedStock _bac;

        public StockMonitorContainerTests()
        {
            var userId = Guid.NewGuid();
            _uat = new StockMonitorContainer();

            _amd = new OwnedStock(new Ticker("AMD"), userId);
            _amd.Purchase(100, 50, DateTimeOffset.Now, null, 49);
            _uat.Register(_amd);

            _bac = new OwnedStock(new Ticker("BAC"), userId);
            _bac.Purchase(100, 50, DateTimeOffset.Now, null, 49);
            _uat.Register(_bac);

            _uat.Register(_amd);
            _uat.Register(_bac);
            _uat.Register(_amd);

            _initialTriggers = _uat.RunCheck("AMD", 50, DateTimeOffset.UtcNow).ToList();
            _subsequentTriggers = _uat.RunCheck("AMD", 48.9m, DateTimeOffset.UtcNow).ToList();
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

            Assert.Equal(48.9m, t.triggeredValue);
        }

        [Fact]
        public void TriggeredCheck()
        {
            Assert.True(_uat.HasTriggered(_amd.State.Ticker, _amd.State.UserId));
            Assert.False(_uat.HasTriggered(_bac.State.Ticker, _bac.State.UserId));
        }

    }
}