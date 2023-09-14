using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.Account;
using core.fs.Accounts;
using core.fs.Alerts;
using core.Shared;
using core.Stocks;
using MediatR;
using storage.shared;

namespace web.Utils;

public class MediatRBasedOutbox : IOutbox
{
    private readonly IMediator _mediator;
    private readonly Create.Handler _createHandler;
    private readonly AlertContainer.Handler _alertsAlertContainerHandler;

    public MediatRBasedOutbox(IMediator mediator, Create.Handler createHandler, AlertContainer.Handler alertsAlertContainerHandler)
    {
        _alertsAlertContainerHandler = alertsAlertContainerHandler;
        _createHandler = createHandler;
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
                    _createHandler.Handle(
                        new Create.SendCreateNotifications(
                            userId: u.AggregateId, email: u.Email,
                            firstName: u.Firstname, lastName: u.Lastname, created: u.When)
                    );
                }
                
                if (@event is StockPurchased_v2 sp)
                {
                    _alertsAlertContainerHandler.StockPurchased();
                }
                
                if (@event is StockSold ss)
                {
                    _alertsAlertContainerHandler.Handle(ss);
                }
                
                if (@event is StopPriceSet sps)
                {
                    _alertsAlertContainerHandler.Handle(sps);
                }
                
                if (@event is StopDeleted sd)
                {
                    _alertsAlertContainerHandler.Handle(sd);
                }
                
                _mediator.Publish(notification);
            }
        }
        
        return Task.FromResult(new ServiceResponse());
    }
}