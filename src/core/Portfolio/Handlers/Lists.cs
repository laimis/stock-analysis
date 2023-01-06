﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Stocks.View;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class Lists
    {
        public class Query : RequestWithUserId<StockListState[]>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, StockListState[]>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<StockListState[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var lists = await _portfolioStorage.GetStockLists(user.State.Id);
                return lists.Select(x => x.State).ToArray();
            }
        }
    }

    public class ListsGet
    {
        public class Query : RequestWithUserId<StockListState>
        {
            public Query(string name, Guid userId) : base(userId)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public class Handler : IRequestHandler<Query, StockListState>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<StockListState> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var list = await _portfolioStorage.GetStockList(request.Name, user.State.Id);
                return list?.State;
            }
        }
    }
}