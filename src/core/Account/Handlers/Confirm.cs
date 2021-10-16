using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Confirm
    {
        public class Command : IRequest<CommandResponse<User>>
        {
            public Guid Id { get; set; }

            public Command(Guid id)
            {
                Id = id;
            }
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
                var r = await _storage.GetUserAssociation(cmd.Id);
                if (r == null)
                {
                    return CommandResponse<User>.Failed(
                        "Invalid confirmation identifier.");
                }

                if (r.IsOlderThan(60 * 24 * 30)) // 30 day expiration?
                {
                    return CommandResponse<User>.Failed(
                        "Account confirmation link is expired. Please request a new one.");
                }

                var u = await _storage.GetUser(r.UserId);
                if (u == null)
                {
                    return CommandResponse<User>.Failed(
                        "User account is no longer valid");
                }

                u.Confirm();

                await _storage.Save(u);

                return CommandResponse<User>.Success(u);
            }
        }
    }
}