using System;
using System.Collections.Generic;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private Dictionary<Guid, StockMonitor> _monitors = new Dictionary<Guid, StockMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();

        public void Monitor(Alert a)
        {
            _tickers.Add(a.State.Ticker);

            foreach(var pp in a.PricePoints)
            {
                if (!_monitors.ContainsKey(pp.Id))
                {
                    _monitors[pp.Id] = new StockMonitor(a, pp);
                }
            }
        }

        public IEnumerable<string> GetTickers()
        {
            return _tickers;
        }

        public IEnumerable<StockMonitorTrigger> UpdateValue(string ticker, double newPrice)
        {
            foreach (var m in _monitors.Values)
            {
                if (m.UpdateValue(ticker, newPrice))
                {
                    yield return new StockMonitorTrigger(m.Alert, newPrice, DateTimeOffset.UtcNow);
                }
            }
        }
    }
}