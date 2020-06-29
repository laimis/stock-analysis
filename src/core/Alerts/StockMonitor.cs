using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Alerts
{
    public class StockMonitor
    {
        public StockMonitor(Alert alert)
        {
            this.Alert = alert;

            this.PointValues = alert.PricePoints.ToDictionary(
                k => k.Id,
                k => (double?)null
            );
        }

        public Alert Alert { get; }
        public Dictionary<Guid, double?> PointValues { get; }

        public bool UpdateValue(string ticker, double newValue)
        {
            if (this.Alert.State.Ticker != ticker)
            {
                return false;
            }

            var triggered = false;

            foreach(var pp in this.Alert.State.PricePoints)
            {
                var local = this.PointValues[pp.Id];
                if (local == null)
                {
                    this.PointValues[pp.Id] = newValue;
                    continue;
                }
                
                var prev = local.Value < pp.Value;
                var curr = newValue < pp.Value;

                if (prev != curr)
                {
                    triggered = true;
                }
            }

            return triggered;
        }
    }
}