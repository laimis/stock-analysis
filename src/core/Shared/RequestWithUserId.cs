using MediatR;

namespace core.Shared
{
    public class RequestWithUserId<T> : IRequest<T>
    {
        public RequestWithUserId(string userId)
        {
            UserId = userId;
        }

        public string UserId { get; }
    }
}