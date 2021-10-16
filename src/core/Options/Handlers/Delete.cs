using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Delete
    {
        public class Command : RequestWithUserId<CommandResponse<OwnedOption>>
        {
            public Command(Guid optionId, Guid userId)
            {
                Id = optionId;
                WithUserId(userId);
            }

            public Guid Id { get; set; }
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
                var option = await _storage.GetOwnedOption(cmd.Id, cmd.UserId);
                if (option == null)
                {
                    return CommandResponse<OwnedOption>.Failed("Trying to delete not owned option");
                }

                option.Delete();

                await _storage.Save(option, cmd.UserId);

                return CommandResponse<OwnedOption>.Success(option);
            }
        }
    }
}