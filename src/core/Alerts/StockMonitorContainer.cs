using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using core.Stocks;

namespace core.Alerts
{
    public class StockMonitorContainer
    {
        private ConcurrentBag<IStockPositionMonitor> _monitors = new ConcurrentBag<IStockPositionMonitor>();
        private HashSet<string> _tickers = new HashSet<string>();

        public IEnumerable<IStockPositionMonitor> Monitors => _monitors;

        public void Register(OwnedStock stock)
        {
            _tickers.Add(stock.State.Ticker);

            var stopMonitor = StopPriceMonitor.CreateIfApplicable(stock.State);
            if (stopMonitor != null)
            {
                _monitors.Add(stopMonitor);
            }

            var profitMonitor = ProfitPriceMonitor.CreateIfApplicable(stock.State);
            if (profitMonitor != null)
            {
                _monitors.Add(profitMonitor);
            }
        }

        private static string ToKey(OwnedStock stock) => ToKey(stock.State.Ticker, stock.State.UserId);
        private static string ToKey(string ticker, Guid userId) => userId.ToString() + ticker;

        internal void Deregister(OwnedStock stock)
        {
            IStockPositionMonitor toRemove = null;
            foreach (var monitor in _monitors)
            {
                if (monitor.Ticker == stock.State.Ticker && monitor.UserId == stock.State.UserId)
                {
                    toRemove = monitor;
                    break;
                }
            }

            if (toRemove != null)
            {
                _monitors.TryTake(out toRemove);
            }
        }

        public IEnumerable<string> GetTickers() => _tickers;

        public IEnumerable<TriggeredAlert> RunCheck(
            string ticker,
            decimal newPrice,
            DateTimeOffset time)
        {
            foreach (var m in _monitors)
            {
                if (m.RunCheck(ticker, newPrice, time))
                {
                    yield return m.TriggeredAlert.Value;
                }
            }
        }

        public bool HasTriggered(string ticker, Guid userId)
        {
            return _monitors.Where(m => m.Ticker == ticker && m.UserId == userId)
                .Where(m => m.IsTriggered)
                .Any();
        }
    }
}