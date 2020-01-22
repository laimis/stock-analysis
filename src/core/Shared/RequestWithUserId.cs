using MediatR;

namespace core.Shared
{
    public class RequestWithUserId<T> : RequestWithUserIdBase, IRequest<T>
    {
        public RequestWithUserId() {}
        public RequestWithUserId(string userId) : base(userId)
        {
        }
    }

    public class RequestWithUserId : RequestWithUserIdBase, IRequest
    {
        public RequestWithUserId() {}

        public RequestWithUserId(string userId) : base(userId)
        {
        }
    }

    public class RequestWithUserIdBase
    {
        public RequestWithUserIdBase(){}

        public RequestWithUserIdBase(string userId)
        {
            UserId = userId;
        }

        public string UserId { get; private set; }
        public void WithUserId(string userId) => UserId = userId;
    }
}