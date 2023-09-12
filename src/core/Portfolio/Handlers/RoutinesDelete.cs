using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class RoutinesDelete
    {
        public class Command : RequestWithUserId<RoutineState>
        {
            public string Name { get; private set; }

            public Command(string name, Guid userId) : base(userId)
            {
                Name = name;
            }
        }

        public class Handler : IRequestHandler<Command, RoutineState>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<RoutineState> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(cmd.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var routine = await _portfolioStorage.GetRoutine(cmd.Name, user.State.Id);
                if (routine == null)
                {
                    throw new InvalidOperationException("Routine does not exists");
                }

                await _portfolioStorage.DeleteRoutine(routine, user.State.Id);

                return routine.State;
            }
        }
    }
}