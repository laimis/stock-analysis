using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Expire
    {
        public class Command : RequestWithUserId
        {
            [Required]
            public Guid? Id { get; set; }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var option = await _storage.GetOwnedOption(cmd.Id.Value, cmd.UserId);
                if (option == null)
                {
                    throw new InvalidOperationException("Trying to expore not owned option");
                }

                option.Expire();

                await this._storage.Save(option, cmd.UserId);

                return new Unit();
            }
        }
    }
}