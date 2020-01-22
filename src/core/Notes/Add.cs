using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Notes
{
    public class Add
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public string Note { get; set; }
            [Required]
            public string RelatedToTicker { get; set; }
            public double? PredictedPrice { get; set; }
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
                    request.RelatedToTicker,
                    request.PredictedPrice);

                await _storage.Save(note);

                return new Unit();
            }
        }
    }
}