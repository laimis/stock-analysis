using System;
using MediatR;

namespace core.Shared
{
    public class RequestWithUserId<T> : RequestWithUserIdBase, IRequest<T>
    {
        public RequestWithUserId() {}
        public RequestWithUserId(Guid userId) : base(userId)
        {
        }
    }

    public class RequestWithUserId : RequestWithUserIdBase, IRequest
    {
        public RequestWithUserId() {}

        public RequestWithUserId(Guid userId) : base(userId)
        {
        }
    }

    public class RequestWithUserIdBase
    {
        public RequestWithUserIdBase(){}

        public RequestWithUserIdBase(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; private set; }
        public void WithUserId(Guid userId) => UserId = userId;
    }
}