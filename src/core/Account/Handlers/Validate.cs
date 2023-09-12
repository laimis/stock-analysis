using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Account.Handlers
{
    public class Validate
    {
        public class Command : Create.UserInfo
        {
        }

        public class Handler : IRequestHandler<Command, CommandResponse<User>>
        {
            private IAccountStorage _storage;
            public Handler(IAccountStorage storage)
            {
                _storage = storage;
            }

            public async Task<CommandResponse<User>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var exists = await _storage.GetUserByEmail(cmd.Email);
                if (exists != null)
                {
                    return CommandResponse<User>.Failed($"Account with {cmd.Email} already exists");
                }

                return CommandResponse<User>.Success(null);
            }
        }
    }
}