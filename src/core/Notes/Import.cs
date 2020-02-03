using System;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Import
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            public Command(string content)
            {
                this.Content = content;
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
                var records = _parser.Parse<NoteRecord>(request.Content);

                foreach(var r in records)
                {
                    var c = new core.Notes.Add.Command{
                        Note = r.note,
                        Ticker = r.ticker,
                        Created = r.created,
                    };

                    c.WithUserId(request.UserId);

                    var ar = await _mediator.Send(c);

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