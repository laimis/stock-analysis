using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.fs.Shared;
using core.Shared;
using storage.shared;

namespace storagetests
{
    public class FakeOutbox : IOutbox
    {
        public Task<ServiceResponse> AddEvents(List<AggregateEvent> e, IDbTransaction tx)
        {
            return Task.FromResult(ServiceResponse.Ok);
        }
    }
}