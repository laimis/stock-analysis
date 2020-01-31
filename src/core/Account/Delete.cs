using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Delete
    {
        public class Command : RequestWithUserId
        {
            public string Feedback { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    return new Unit();
                }

                user.Delete();

                await _storage.Save(user);
                
                return new Unit();
            }
        }
    }
}