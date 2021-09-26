using System;
using MediatR;

namespace core.Shared
{
    public class ScheduleUserChanged : INotification
    {
        public ScheduleUserChanged(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}