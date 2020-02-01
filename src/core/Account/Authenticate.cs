using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Account
{
    public class Authenticate
    {
        public class Command : IRequest<AuthenticateResult>
        {
            [Required]
            public string Email { get; set; }
            [Required]
            public string Password { get; set; }
            public string IPAddress { get; private set; }

            public void WithIPAddress(string ipAddress)
            {
                this.IPAddress = ipAddress;
            }
        }

        public class Handler : IRequestHandler<Command, AuthenticateResult>
        {
            private const string GENERIC_MSG = "Invalid email/password combination";
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hashProvider)
            {
                _storage = storage;
                _hash = hashProvider;
            }

            public async Task<AuthenticateResult> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await this._storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    return AuthenticateResult.Failed(GENERIC_MSG);
                }

                // oauth path where password was not set....
                if (user.State.GetSalt() == null)
                {
                    return AuthenticateResult.Failed(GENERIC_MSG);
                }

                var computed = _hash.Generate(request.Password, user.State.GetSalt());

                var matches = user.PasswordHashMatches(computed);

                if (matches)
                {
                    return AuthenticateResult.Success(user);
                }

                return AuthenticateResult.Failed(GENERIC_MSG);
            }
        }
    }
}