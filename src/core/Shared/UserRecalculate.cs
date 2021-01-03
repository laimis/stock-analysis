using System;
using MediatR;

namespace core.Shared
{
    public class UserRecalculate : INotification
    {
        public UserRecalculate(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}