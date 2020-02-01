using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Emails;
using MediatR;

namespace core.Account
{
    public class ResetPassword
    {
        public class Command : IRequest<ResetPasswordResult>
        {
            [Required]
            public Guid? Id { get; set; }
            [Required]
            [MinLength(12)]
            [MaxLength(1000)]
            public string Password { get; set; }
        }

        public class Handler : MediatR.IRequestHandler<Command, ResetPasswordResult>
        {
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;

            public Handler(IAccountStorage storage, IPasswordHashProvider hash)
            {
                _storage = storage;
                _hash = hash;
            }

            public async Task<ResetPasswordResult> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var r = await _storage.GetPasswordResetRequest(cmd.Id.Value);
                if (r == null)
                {
                    return ResetPasswordResult.Failed("Invalid password reset token. Check the link in the email or request a new password reset");
                }

                if (r.IsExpired)
                {
                    return ResetPasswordResult.Failed("Password reset link has expired. Please request a new password reset");
                }

                var u = await _storage.GetUser(r.UserId.ToString());
                if (u == null)
                {
                    return ResetPasswordResult.Failed("User account is no longer valid");
                }

                var hash = _hash.Generate(cmd.Password, 32);

                u.SetPassword(hash.Hash, hash.Salt);

                await _storage.Save(u);

                return ResetPasswordResult.Success(u);
            }
        }
    }
}