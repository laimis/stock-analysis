using System;
using MediatR;

namespace core.Shared
{
    public class UserStatusRecalculate : INotification
    {
        public UserStatusRecalculate(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}