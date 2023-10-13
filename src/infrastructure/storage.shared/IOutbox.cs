#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.Shared;

namespace storage.shared;

public interface IOutbox
{
    Task<core.fs.Shared.ServiceResponse> AddEvents(List<AggregateEvent> e, IDbTransaction? tx);
}