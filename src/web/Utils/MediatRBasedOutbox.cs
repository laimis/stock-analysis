using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.Shared;
using MediatR;
using storage.shared;

namespace web.Utils;

public class MediatRBasedOutbox : IOutbox
{
    private readonly IMediator _mediator;

    public MediatRBasedOutbox(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public Task<ServiceResponse> AddEvents(List<AggregateEvent> e, IDbTransaction tx)
    {
        foreach (var @event in e)
        {
            if (@event is INotification notification)
                _mediator.Publish(notification);
        }
        
        return Task.FromResult(new ServiceResponse());
    }
}