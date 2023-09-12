using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.SMS;
using MediatR;

namespace core.Alerts.Handlers
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
            private ISMSClient _smsClient;

            public Handler(ISMSClient smsClient)
            {
                _smsClient = smsClient;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                await _smsClient.SendSMS(request.Body);

                return CommandResponse.Success();
            }
        }
    }
}