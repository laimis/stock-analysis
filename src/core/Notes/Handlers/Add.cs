﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Add
    {
        public class Command : RequestWithTicker<CommandResponse<Note>>
        {
            [Required]
            public string Note { get; set; }
            public DateTimeOffset? Created { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<Note>>
        {
            private IPortfolioStorage _storage;
            private IAccountStorage _accounts;

            public Handler(
                IAccountStorage accountStorage,
                IPortfolioStorage storage)
            {
                _storage = storage;
                _accounts = accountStorage;
            }

            public async Task<CommandResponse<Note>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse<Note>.Failed(
                        "Unable to find user account for notes operation");
                }

                if (!user.Verified)
                {
                    return CommandResponse<Note>.Failed(
                        "Please verify your email first before you can start creating notes");
                }

                var note = new Note(
                    cmd.UserId,
                    cmd.Note,
                    cmd.Ticker,
                    cmd.Created ?? DateTimeOffset.UtcNow);

                await _storage.Save(note, cmd.UserId);

                return CommandResponse<Note>.Success(note);
            }
        }
    }
}