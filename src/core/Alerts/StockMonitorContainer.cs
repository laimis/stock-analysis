using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private ConcurrentDictionary<Guid, StockMonitor> _monitors = new ConcurrentDictionary<Guid, StockMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();

        public IEnumerable<StockMonitor> Monitors => _monitors.Values;

        public void Register(Alert a)
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

        internal void Deregister(Guid pricePointId)
        {
            _monitors.TryRemove(pricePointId, out var val);
        }

        public IEnumerable<string> GetTickers()
        {
            return _tickers;
        }

        public IEnumerable<StockMonitorTrigger> UpdateValue(
            string ticker,
            double newPrice,
            DateTimeOffset time)
        {
            foreach (var m in _monitors.Values)
            {
                if (m.CheckTrigger(ticker, newPrice, time, out var trigger))
                {
                    yield return trigger;
                }
            }
        }

        public bool HasTriggered(AlertPricePoint pp)
        {
            _monitors.TryGetValue(pp.Id, out var m);

            return m != null && m.IsTriggered;
        }
    }
}