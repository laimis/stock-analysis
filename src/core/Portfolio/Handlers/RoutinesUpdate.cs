﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class RoutinesUpdate
    {
        public class Command : RequestWithUserId<RoutineState>
        {
            [Required]
            public string Name { get; set; }
            [Required]
            public string NewName { get; set; }
            public string Description { get; set; }
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
                    throw new InvalidOperationException("Routine does not exist");
                }

                routine.Update(name: cmd.NewName, description: cmd.Description);

                await _portfolioStorage.Save(routine, user.State.Id);

                return routine.State;
            }
        }
    }
}