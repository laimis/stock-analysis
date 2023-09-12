using System;
using System.Threading;
using System.Threading.Tasks;

namespace core.Account.Handlers
{
    public class SignInViaGoogle
    {
        public class Command : MediatR.IRequest<Guid?>
        {
            public Command(string email)
            {
                Email = email;
            }

            public string Email { get; }
        }

        public class Handler : MediatR.IRequestHandler<Command, Guid?>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<Guid?> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    return null;
                }
                return user.State.Id;
            }
        }
    }
}