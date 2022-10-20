using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Account
{
    public class BrokerageConnect
    {
        public class Command : IRequest<string>
        {
        }

        public class Handler : IRequestHandler<Command, string>
        {
            private IBrokerage _brokerage;
            public Handler(IBrokerage brokerage)
            {
                _brokerage = brokerage;
            }

            public Task<string> Handle(Command request, CancellationToken cancellationToken) => _brokerage.GetOAuthUrl();
        }
    }

    public class BrokerageConnectCallback
    {
        public class Command : IRequest<CommandResponse>
        {
            public Command(string code, Guid userId)
            {
                Code = code;
                UserId = userId;
            }

            [Required]
            public string Code { get; }

            [Required]
            public Guid UserId { get; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse>
        {
            private IBrokerage _brokerage;
            private IAccountStorage _storage;

            public Handler(IBrokerage brokerage, IAccountStorage storage)
            {
                _brokerage = brokerage;
                _storage = storage;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var r = await _brokerage.ConnectCallback(request.Code);

                var user = await _storage.GetUser(request.UserId);

                user.ConnectToBrokerage(r.access_token, r.refresh_token, r.token_type, r.expires_in, r.scope, r.refresh_token_expires_in);

                await _storage.Save(user);
                
                return CommandResponse.Success();
            }
        }
    }

    public class BrokerageDisconnect
    {
        public class Command : IRequest<CommandResponse>
        {
            public Command(Guid userId) => UserId = userId;

            [Required]
            public Guid UserId { get; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse>
        {
            private IBrokerage _brokerage;
            private IAccountStorage _storage;

            public Handler(IBrokerage brokerage, IAccountStorage storage)
            {
                _brokerage = brokerage;
                _storage = storage;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);

                user.DisconnectFromBrokerage();

                await _storage.Save(user);
                
                return CommandResponse.Success();
            }
        }
    }

    public class BrokerageInfo
    {
        public class Query : IRequest<object>
        {
            public Query(Guid userId) => UserId = userId;

            [Required]
            public Guid UserId { get; }
        }

        public class Handler : IRequestHandler<Query, object>
        {
            private IBrokerage _brokerage;
            private IAccountStorage _storage;

            public Handler(IBrokerage brokerage, IAccountStorage storage)
            {
                _brokerage = brokerage;
                _storage = storage;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);

                return await _brokerage.GetAccessToken(user.State);
            }
        }
    }
}