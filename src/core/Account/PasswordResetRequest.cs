using System;

namespace core.Account
{
    public class PasswordResetRequest
    {
        private PasswordResetRequest(Guid id, Guid userId, DateTimeOffset timestamp)
        {
            this.Id = id;
            this.UserId = userId;
            this.Timestamp = timestamp;
        }

        public PasswordResetRequest(Guid userId, DateTimeOffset timestamp) 
            : this(Guid.NewGuid(), userId, timestamp)
        {
        }

        public PasswordResetRequest(Guid id, Guid userId, string timestamp)
            : this(id, userId, DateTimeOffset.Parse(timestamp))
        {
        }

        public Guid Id { get; }
        public Guid UserId { get; }
        public DateTimeOffset Timestamp { get; }
        public bool IsExpired => DateTimeOffset.UtcNow.Subtract(this.Timestamp).TotalMinutes > 15;
    }
}