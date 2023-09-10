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
            [Obsolete($"Use {nameof(AssignedCommand)} or {nameof(UnassignedCommand)}")]
            // ReSharper disable once MemberCanBeProtected.Global - it's created via controller
            public Command() {}
            [Required]
            public Guid? Id { get; set; }
        }

        public class AssignedCommand : Command
        {
            public bool Assigned => true;
        }

        public class UnassignedCommand : Command
        {
            public bool Assigned => false;
        }

        public class Handler : IRequestHandler<AssignedCommand, CommandResponse<OwnedOption>>,
            IRequestHandler<UnassignedCommand, CommandResponse<OwnedOption>>
        {
            private readonly IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public Task<CommandResponse<OwnedOption>> Handle(AssignedCommand cmd, CancellationToken _)
                => HandleInternal(cmd.Id!.Value, cmd.Assigned, cmd.UserId);
            
            public Task<CommandResponse<OwnedOption>> Handle(UnassignedCommand cmd, CancellationToken _)
                => HandleInternal(cmd.Id!.Value, cmd.Assigned, cmd.UserId);

            private async Task<CommandResponse<OwnedOption>> HandleInternal(Guid optionId, bool assigned, Guid userId)
            {
                var option = await _storage.GetOwnedOption(optionId, userId);
                if (option == null)
                {
                    return CommandResponse<OwnedOption>.Failed("Trying to expire not owned option");
                }

                option.Expire(assigned);

                await _storage.Save(option, userId);

                return CommandResponse<OwnedOption>.Success(option);
            }
        }
    }
}