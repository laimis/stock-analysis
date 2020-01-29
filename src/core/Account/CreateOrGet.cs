using System;
using System.Threading;
using System.Threading.Tasks;

namespace core.Account
{
    public class CreateOrGet
    {
        public class Command : MediatR.IRequest<Guid>
        {
            public Command(string email)
            {
                this.Email = email;
            }

            public string Email { get; }
        }

        public class Handler : MediatR.IRequestHandler<Command, Guid>
        {
            private IAccountStorage _storage;

            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<Guid> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.Email);
                if (user == null)
                {
                    user = new User(request.Email);
                    await _storage.Save(user);
                }
                return user.State.Id;
            }
        }
    }
}