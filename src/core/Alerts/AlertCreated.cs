using System;
using core.Shared;

namespace core.Alerts
{
    internal class AlertCreated : AggregateEvent
    {
        public AlertCreated(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, Guid userId, double threshold, bool dailyReport)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.UserId = userId;
            this.Threshold = threshold;
            this.DailyReport = dailyReport;
        }

        public string Ticker { get; }
        public Guid UserId { get; }
        public double Threshold { get; }
        public bool DailyReport { get; }
    }
}