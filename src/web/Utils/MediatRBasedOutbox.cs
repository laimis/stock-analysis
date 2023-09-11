using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using core.Shared;
using MediatR;
using storage.shared;
using OptionTransactionHandler = core.fs.Options.OptionTransactionHandler;

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

public class OptionTransactionHandlerViaMediatR :
    INotificationHandler<OptionSold>,
    INotificationHandler<OptionPurchased>
{
    private readonly OptionTransactionHandler _handler;

    public OptionTransactionHandlerViaMediatR(OptionTransactionHandler handler)
    {
        _handler = handler;
    }

    public Task Handle(OptionSold notification, CancellationToken cancellationToken) =>
        _handler.HandleSell(notification);

    public Task Handle(OptionPurchased notification, CancellationToken cancellationToken) =>
        _handler.HandleBuy(notification);
}