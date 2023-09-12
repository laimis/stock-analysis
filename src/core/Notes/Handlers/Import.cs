using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.CSV;
using MediatR;

namespace core.Notes.Handlers
{
    public class Import
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                Content = content;
            }

            public string Content { get; }
        }

        public class Handler : MediatR.IRequestHandler<Command, CommandResponse>
        {
            private IMediator _mediator;
            private ICSVParser _parser;

            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
            {
                var parseResponse = _parser.Parse<NoteRecord>(request.Content);
                if (!parseResponse.IsOk)
                {
                    return CommandResponse.Failed(parseResponse.Error!.Message);
                }

                foreach(var r in parseResponse.Success)
                {
                    var c = new Add.Command{
                        Note = r.note,
                        Ticker = r.ticker,
                        Created = r.created,
                    };

                    c.WithUserId(request.UserId);

                    var ar = await _mediator.Send(c, cancellationToken);

                    if (ar.Error != null)
                    {
                        return ar;
                    }
                }

                return CommandResponse.Success();
            }

            private class NoteRecord
            {
                public DateTimeOffset created { get; set; }
                public string ticker { get; set; }
                public string note { get; set; }
            }
        }
    }
}