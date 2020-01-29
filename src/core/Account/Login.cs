using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Login
    {
        public class Command : RequestWithUserId
        {
            public Command(string userId, string ipAddress) : base(userId)
            {
                this.IPAddress = ipAddress;
                this.Timestamp = DateTimeOffset.UtcNow;
            }

            public string IPAddress { get; }
            public DateTimeOffset? Timestamp { get; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                // var user = await this._storage.GetUser()

                return Task.FromResult(new Unit());
            }
        }
    }
}