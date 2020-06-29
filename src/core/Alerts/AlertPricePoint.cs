using System;

namespace core.Alerts
{
    public struct AlertPricePoint
    {
        public AlertPricePoint(Guid id, double value)
        {
            Id = id;
            Value = value;
        }

        public Guid Id { get; }
        public double Value { get; }
    }
}