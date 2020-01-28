using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Options
{
    public class Get
    {
        public class Query : RequestWithUserId<object>
        {
            [Required]
            public Guid Id { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {

            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var sold = await _storage.GetSoldOption(request.Id, request.UserId);
                
                return Mapper.ToOptionView(sold);
            }
        }
    }
}