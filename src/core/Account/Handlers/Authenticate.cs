using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Authenticate
    {
        public class Command : IRequest<CommandResponse<User>>
        {
            [Required]
            public string Email { get; set; }
            [Required]
            public string Password { get; set; }
            public string IPAddress { get; private set; }

            public void WithIPAddress(string ipAddress)
            {
                IPAddress = ipAddress;
            }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<User>>
        {
            private const string GENERIC_MSG = "Invalid email/password combination";
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hashProvider)
            {
                _storage = storage;
                _hash = hashProvider;
            }

            public async Task<CommandResponse<User>> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    return CommandResponse<User>.Failed(GENERIC_MSG);
                }

                // oauth path where password was not set....
                if (!user.State.IsPasswordAvailable)
                {
                    return CommandResponse<User>.Failed(GENERIC_MSG);
                }

                var computed = _hash.Generate(request.Password, user.State.GetSalt());

                var matches = user.PasswordHashMatches(computed);

                if (matches)
                {
                    user.LoggedIn(request.IPAddress, DateTimeOffset.UtcNow);

                    await _storage.Save(user);

                    return CommandResponse<User>.Success(user);
                }

                return CommandResponse<User>.Failed(GENERIC_MSG);
            }
        }
    }
}