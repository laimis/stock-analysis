using System;

namespace core.Shared.Adapters.Brokerage
{
    public interface IMarketHours
    {
        bool IsMarketOpen(DateTimeOffset time);
        DateTimeOffset ToMarketTime(DateTimeOffset utc);
        DateTimeOffset ToUniversalTime(DateTimeOffset eastern);
        DateTimeOffset GetMarketEndOfDayTimeInUtc(DateTimeOffset when);
        DateTimeOffset GetMarketStartOfDayTimeInUtc(DateTimeOffset when);
    }
}