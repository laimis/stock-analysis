using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.fs;
using core.Shared;
using Microsoft.FSharp.Core;
using storage.shared;

namespace storagetests
{
    public class FakeOutbox : IOutbox
    {
        public Task<FSharpResult<Unit,ServiceError>> AddEvents(List<AggregateEvent> e, IDbTransaction tx) =>
            Task.FromResult(FSharpResult<Unit, ServiceError>.NewOk(null));
    }
}
