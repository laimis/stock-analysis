using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.Account;
using core.fs.Account;
using core.Shared;
using MediatR;
using storage.shared;

namespace web.Utils;

public class MediatRBasedOutbox : IOutbox
{
    private readonly IMediator _mediator;
    private readonly Create.Handler _handler;

    public MediatRBasedOutbox(IMediator mediator, Create.Handler handler)
    {
        _handler = handler;
        _mediator = mediator;
    }
    
    public Task<ServiceResponse> AddEvents(List<AggregateEvent> e, IDbTransaction tx)
    {
        foreach (var @event in e)
        {
            if (@event is INotification notification)
            {
                if (@event is UserCreated u)
                {
                    _handler.Handle(
                        new Create.SendCreateNotifications(
                            userId: u.AggregateId, email: u.Email,
                            firstName: u.Firstname, lastName: u.Lastname, created: u.When)
                    );
                }
                _mediator.Publish(notification);
            }
        }
        
        return Task.FromResult(new ServiceResponse());
    }
}