using System.Threading;
using System.Threading.Tasks;
using core.Shared.Adapters.SMS;
using MediatR;

namespace core.Alerts.Handlers
{
    public class SmsStatus
    {
        public class Query : IRequest<bool> {}

        public class Handler : IRequestHandler<Query, bool>
        {
            private ISMSClient _client;
            public Handler(ISMSClient client) => _client = client;
            public Task<bool> Handle(Query request, CancellationToken cancellationToken)
                => Task.FromResult(_client.IsOn);
        }
    }

    public class SmsOn
    {
        public class Command : IRequest<Unit> {}

        public class Handler : IRequestHandler<Command, Unit>
        {
            private ISMSClient _client;
            public Handler(ISMSClient client) => _client = client;
            public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _client.TurnOn();
                return Unit.Task;
            }
        }
    }

    public class SmsOff
    {
        public class Command : IRequest<Unit> {}

        public class Handler : IRequestHandler<Command, Unit>
        {
            private ISMSClient _client;
            public Handler(ISMSClient client) => _client = client;
            public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _client.TurnOff();
                return Unit.Task;
            }
        }
    }
}