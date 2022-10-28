using System;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace core.Shared
{
    public class RequestWithTicker<T> : RequestWithUserId<T>
    {
        private Ticker? _ticker;
        [Required]
        public string Ticker 
        {
            get 
            { 
                if (_ticker == null) return null;
                return _ticker;
            }
            
            set 
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _ticker = null;
                    return;
                }
                _ticker = new Ticker(value);
            }
        }
    }

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

        public Guid UserId { get; protected set; }
        public void WithUserId(Guid userId) => UserId = userId;
    }
}