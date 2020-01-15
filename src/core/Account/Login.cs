using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Account
{
    public class Login
    {
        public class Command : IRequest
        {
            public Command(string username)
            {
                this.Username = username;
                this.Date = DateTime.UtcNow;
            }

            public string Username { get; }
            public DateTime Date { get; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var entry = new LoginLogEntry(
                    request.Username,
                    request.Date
                );

                await this._storage.RecordLoginAsync(entry);

                return new Unit();
            }
        }
    }
}