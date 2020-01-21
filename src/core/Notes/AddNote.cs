using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Notes
{
    public class AddNote
    {
        public class Command : IRequest
        {
            [Required]
            public string Note { get; set; }
            public string Ticker { get; set; }
            public string UserId { get; private set; }
            public double PredictedPrice { get; set; }

            public void WithUserId(string userId) => UserId = userId;
        }

        public class Handler : IRequestHandler<Command>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var note = new Note(
                    request.UserId,
                    request.Note,
                    request.Ticker,
                    request.PredictedPrice);

                await _storage.Save(note);

                return new Unit();
            }
        }
    }
}