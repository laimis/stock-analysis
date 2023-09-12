using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class RoutinesRemoveStep
    {
        public class Command : RequestWithUserId<RoutineState>
        {
            public string RoutineName { get; set; }
            public int StepIndex { get; set; }
            public Command(string routineName, int stepIndex, Guid userId) : base(userId)
            {
                RoutineName = routineName;
                StepIndex = stepIndex;
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

                var routine = await _portfolioStorage.GetRoutine(cmd.RoutineName, user.State.Id);
                if (routine == null)
                {
                    throw new InvalidOperationException("Routine does not exist");
                }

                routine.RemoveStep(cmd.StepIndex);

                await _portfolioStorage.Save(routine, user.State.Id);

                return routine.State;
            }
        }
    }
}