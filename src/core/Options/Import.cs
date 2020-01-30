using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.CSV;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Import
    {
        public class Command : RequestWithUserId
        {
            public Command(string content)
            {
                this.Content = content;
            }

            public string Content { get; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private IMediator _mediator;
            private ICSVParser _parser;
            
            public Handler(IMediator mediator, ICSVParser parser)
            {
                _mediator = mediator;
                _parser = parser;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var records = _parser.Parse<OptionRecord>(request.Content);

                foreach(var r in records)
                {
                    await ProcessLine(r, request.UserId);
                }

                return new Unit();
            }

            private Task ProcessLine(OptionRecord record, string userId)
            {
                throw new InvalidOperationException("Needs to be implemented");
            }

            private class OptionRecord
            {
                public DateTimeOffset? closed { get; set; }
                public DateTimeOffset? expiration { get; set; }
                public DateTimeOffset? filled { get; set; }
                public int amount { get; set; }
                public string type { get; set; }
                public double premium { get; set; }
                public double strike { get; set; }
                public string ticker { get; set; }
                public double? spent { get; set; }
            }
        }
    }
}