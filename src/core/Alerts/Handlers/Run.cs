using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;

namespace core.Alerts
{
    public class Run
    {
        public class Command : RequestWithUserId<Unit>
        {
            public Command(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Command, Unit>
        {
            private StockAlertContainer _container;

            public Handler(StockAlertContainer container) => _container = container;

            public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                _container.EnableManualRun();

                return Task.FromResult(Unit.Value);
            }
        }
    }
}