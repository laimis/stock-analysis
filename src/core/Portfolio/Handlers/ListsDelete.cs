﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class ListsDelete
    {
        public class Command : RequestWithUserId
        {
            public Command(string name, Guid userId) : base(userId)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(cmd.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }
                
                var list = await _portfolioStorage.GetStockList(cmd.Name, user.State.Id);
                if (list == null)
                {
                    throw new InvalidOperationException("List does not exists");
                }

                await _portfolioStorage.DeleteStockList(list, user.Id);

                return new Unit();
            }
        }
    }
}