using System;
using MediatR;

namespace core.Shared
{
    public class UserChanged : INotification
    {
        public UserChanged(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }
    }
}