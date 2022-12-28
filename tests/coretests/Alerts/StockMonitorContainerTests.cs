using System;
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
        private OwnedStock _amd;
        private OwnedStock _bac;
        private int _initialCount = 0;
        private int _finalCount = 0;

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

            _initialCount = _uat.Monitors.Count();

            _uat.Register(_amd);
            _uat.Register(_bac);
            _uat.Register(_amd);

            _finalCount = _uat.Monitors.Count();
        }

        [Fact]
        public void MonitorsNotEmpty() => Assert.NotEmpty(_uat.Monitors);

        // checks that registering is idempotent
        [Fact]
        public void MonitorsCountIsCorrect() => Assert.Equal(_initialCount, _finalCount);
    }
}