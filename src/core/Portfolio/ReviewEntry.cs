using System;

namespace core.Portfolio
{
    public struct ReviewEntry
    {
        public string Ticker { get; internal set; }
        public string Description { get; internal set; }
        public DateTimeOffset? Expiration { get; internal set; }
        public TimeSpan? TTL => Expiration == null ? (TimeSpan?)null : DateTimeOffset.UtcNow.Subtract(this.Expiration.Value);
        public bool IsNote { get; internal set; }
    }
}