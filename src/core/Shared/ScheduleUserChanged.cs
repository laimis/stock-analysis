using System;
using MediatR;

namespace core.Shared
{
    public class ScheduleUserChanged : INotification
    {
        public ScheduleUserChanged(Guid userId)
        {
            UserId = userId;
            When = DateTimeOffset.UtcNow;
        }

        public Guid UserId { get; }
        public DateTimeOffset When { get; }
    }
}