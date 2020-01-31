using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Account
{
    public class PasswordReset
    {
        public class Request : IRequest<PasswordResetResult>
        {
            [Required]
            public string Email { get; set; }
            public string IPAddress { get; private set; }

            public void WithIPAddress(string ipAddress)
            {
                this.IPAddress = ipAddress;
            }
        }

        public class Handler : IRequestHandler<Request, PasswordResetResult>
        {
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hashProvider)
            {
                _storage = storage;
                _hash = hashProvider;
            }

            public async Task<PasswordResetResult> Handle(Request request, CancellationToken cancellationToken)
            {
                var user = await this._storage.GetUserByEmail(request.Email);
                if (user == null)
                {
                    return PasswordResetResult.Failed("User does not exist");
                }

                user.RequestPasswordReset(DateTimeOffset.UtcNow);

                await this._storage.Save(user);

                return PasswordResetResult.Success();
            }
        }
    }
}