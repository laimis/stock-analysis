using System;
using System.Threading;
using System.Threading.Tasks;

namespace core.Account
{
    public class CreateOrGet
    {
        public class Command : MediatR.IRequest<Guid>
        {
            public Command(string email, string firstname, string lastname)
            {
                this.Email = email;
                this.Firstname = firstname;
                this.Lastname = lastname;
            }

            public string Email { get; }
            public string Firstname { get; }
            public string Lastname { get; }
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
                var user = await _storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    user = new User(request.Email, request.Firstname, request.Lastname);
                    await _storage.Save(user);
                }
                return user.State.Id;
            }
        }
    }
}