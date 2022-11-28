using System;

namespace core.Shared.Adapters.Brokerage
{
    public interface IMarketHours
    {
        bool IsMarketOpen(DateTimeOffset time);
        DateTimeOffset ToMarketTime(DateTimeOffset when);
        DateTimeOffset GetMarketEndOfDayTimeInUtc(DateTimeOffset when);
    }
}