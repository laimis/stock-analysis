using System;

namespace core.Shared
{
    public class AggregateEvent
    {
        public AggregateEvent(string key, string userId, DateTime when)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidOperationException("key cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException("userId cannot be empty");
            }

            this.Ticker = key;
            this.UserId = userId;
            this.When = when;
        }

        public DateTime When { get; }
        public string Ticker { get; }
        public string UserId { get; }
    }
}