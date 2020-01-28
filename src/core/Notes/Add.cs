using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Add
    {
        public class Command : RequestWithUserId<object>
        {
            [Required]
            public string Note { get; set; }
            [Required]
            public string Ticker { get; set; }
            public double? PredictedPrice { get; set; }
        }

        public class Handler : IRequestHandler<Command, object>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }

            public async Task<object> Handle(Command request, CancellationToken cancellationToken)
            {
                var note = new Note(
                    request.UserId,
                    request.Note,
                    request.Ticker,
                    request.PredictedPrice);

                await _storage.Save(note, request.UserId);

                return new {
                    id = note.State.Id
                };
            }
        }
    }
}