using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Account.Handlers
{
    public class ResetPassword
    {
        public class Command : IRequest<CommandResponse<User>>
        {
            [Required]
            public Guid? Id { get; set; }
            [Required]
            [MinLength(10)]
            [MaxLength(1000)]
            public string Password { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command, CommandResponse<User>>
        {
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hash)
            {
                _storage = storage;
                _hash = hash;
            }

            public async Task<CommandResponse<User>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var r = await _storage.GetUserAssociation(cmd.Id.Value);
                if (r == null)
                {
                    return CommandResponse<User>.Failed(
                        "Invalid password reset token. Check the link in the email or request a new password reset");
                }

                if (r.IsOlderThan(15))
                {
                    return CommandResponse<User>.Failed(
                        "Password reset link has expired. Please request a new password reset");
                }

                var u = await _storage.GetUser(r.UserId);
                if (u == null)
                {
                    return CommandResponse<User>.Failed(
                        "User account is no longer valid");
                }

                var hash = _hash.Generate(cmd.Password, 32);

                u.SetPassword(hash.Hash, hash.Salt);

                await _storage.Save(u);

                return CommandResponse<User>.Success(u);
            }
        }
    }
}