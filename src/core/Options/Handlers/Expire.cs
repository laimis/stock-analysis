using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Expire
    {
        public class Command : RequestWithUserId<CommandResponse<OwnedOption>>
        {
            [Required]
            public Guid? Id { get; set; }

            [Required]
            public bool? Assigned { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<OwnedOption>>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<CommandResponse<OwnedOption>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var option = await _storage.GetOwnedOption(cmd.Id.Value, cmd.UserId);
                if (option == null)
                {
                    return CommandResponse<OwnedOption>.Failed("Trying to expire not owned option");
                }

                option.Expire(cmd.Assigned.Value);

                await _storage.Save(option, cmd.UserId);

                return CommandResponse<OwnedOption>.Success(option);
            }
        }
    }
}