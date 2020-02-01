using System;

namespace core.Account
{
    public class ProcessIdToUserAssociation
    {
        private ProcessIdToUserAssociation(Guid id, Guid userId, DateTimeOffset timestamp)
        {
            this.Id = id;
            this.UserId = userId;
            this.Timestamp = timestamp;
        }

        public ProcessIdToUserAssociation(Guid userId, DateTimeOffset timestamp) 
            : this(Guid.NewGuid(), userId, timestamp)
        {
        }

        public ProcessIdToUserAssociation(Guid id, Guid userId, string timestamp)
            : this(id, userId, DateTimeOffset.Parse(timestamp))
        {
        }

        public Guid Id { get; }
        public Guid UserId { get; }
        public DateTimeOffset Timestamp { get; }
        public bool IsOlderThan(long minutes) => DateTimeOffset.UtcNow.Subtract(this.Timestamp).TotalMinutes > minutes;
    }
}