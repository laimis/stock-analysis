﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class RoutinesAddStep
    {
        public class Command : RequestWithUserId<RoutineState>
        {
            [Required]
            public string RoutineName { get; set; }
            [Required]
            public string Label { get; set; }
            public string Url { get; set; }
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

                routine.AddStep(label: cmd.Label, url: cmd.Url);

                await _portfolioStorage.Save(routine, user.State.Id);

                return routine.State;
            }
        }
    }
}