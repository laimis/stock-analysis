#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.fs;
using core.Shared;
using Microsoft.FSharp.Core;

namespace storage.shared
{
    public interface IOutbox
    {
        Task<FSharpResult<Unit,ServiceError>> AddEvents(List<AggregateEvent> e, IDbTransaction? tx);
    }
}
