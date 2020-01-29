using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Close
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public Guid? Id { get; set; }

            [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
            public int Amount { get; set; }

            [Required]
            [Range(0, 1000)]
            public double? ClosePrice { get; set; }

            [Required]
            public DateTimeOffset? CloseDate { get; set; }
        }

        public class Handler : IRequestHandler<Command>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }

            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                Console.WriteLine("received command " + cmd.Id);
                
                var sold = await _storage.GetSoldOption(
                    cmd.Id.Value,
                    cmd.UserId);

                if (sold != null)
                {
                    Console.WriteLine("closing option");

                    sold.Close(cmd.Amount, cmd.ClosePrice.Value, cmd.CloseDate.Value);
                    await _storage.Save(sold, cmd.UserId);
                }

                Console.WriteLine("end");

                return new Unit();
            }
        }
    }
}