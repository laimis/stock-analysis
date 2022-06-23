using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    public class SendSMS
    {
        public class Command : IRequest<CommandResponse>
        {
            public Command(string body)
            {
                Body = body;
            }

            public string Body { get; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse>
        {
            public Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                Console.WriteLine("Received: " + request.Body);
                return Task.FromResult(CommandResponse.Success());
            }
        }
    }
}