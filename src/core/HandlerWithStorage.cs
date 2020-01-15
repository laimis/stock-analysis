using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core
{
    public abstract class HandlerWithStorage<Input, Output> : IRequestHandler<Input, Output>
        where Input : IRequest<Output>
    {
        protected IPortfolioStorage _storage;

        public HandlerWithStorage(IPortfolioStorage storage)
        {
            _storage = storage;
        }

        public abstract Task<Output> Handle(Input request, CancellationToken cancellationToken);
    }
}