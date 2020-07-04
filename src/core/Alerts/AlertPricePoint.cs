using System;

namespace core.Alerts
{
    public struct AlertPricePoint
    {
        public AlertPricePoint(Guid id, string description, double value)
        {
            Id = id;
            Description = description;
            Value = value;
        }

        public Guid Id { get; }
        public string Description { get; }
        public double Value { get; }
    }
}