using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using core.fs;
using core.Shared;
using Microsoft.FSharp.Core;
using storage.shared;

namespace web.Utils;

public class IncompleteOutbox : IOutbox
{
    // TODO: will need to build proper outbox
    // this used to shuffle the events to mediatr and then to the handlers
    // but it was without any ability to handle failures etc and dependent on
    // mediatr. I might still use simple outbox that does not offer failure handling
    // but it will not be dependent on mediatr
    public Task<FSharpResult<Unit,ServiceError>> AddEvents(List<AggregateEvent> e, IDbTransaction tx)
    {
        foreach (var @event in e)
        {
            // if (@event is INotification notification)
            // {
                // TODO: these need to be brought back vai real outbox
                // if (@event is UserCreated u)
                // {
                //     _createHandler.Handle(
                //         new Create.SendCreateNotifications(
                //             userId: u.AggregateId, email: u.Email,
                //             firstName: u.Firstname, lastName: u.Lastname, created: u.When)
                //     );
                // }
                
                // if (@event is StockPurchased_v2 sp)
                // {
                //     _alertsAlertContainerHandler.StockPurchased();
                // }
                //
                // if (@event is StockSold ss)
                // {
                //     _alertsAlertContainerHandler.Handle(ss);
                // }
                //
                // if (@event is StopPriceSet sps)
                // {
                //     _alertsAlertContainerHandler.Handle(sps);
                // }
                //
                // if (@event is StopDeleted sd)
                // {
                //     _alertsAlertContainerHandler.Handle(sd);
                // }
                
            //     _mediator.Publish(notification);
            // }
        }

        return Task.FromResult(FSharpResult<Unit, ServiceError>.NewOk(null));
    }
}

// public class StockTransactionHandler : 
//         MediatR.INotificationHandler<StockSold>,
//         MediatR.INotificationHandler<StockPurchased>,
//         MediatR.INotificationHandler<StockPurchased_v2>
//     {
//         private IPortfolioStorage _storage;
//         private IMediator _mediator;
//
//         public StockTransactionHandler(IPortfolioStorage storage, IMediator mediator)
//         {
//             _storage = storage;
//             _mediator = mediator;
//         }
//
//         public async Task Handle(StockSold e, CancellationToken cancellationToken)
//         {
//             var s = await _storage.GetStock(e.Ticker, e.UserId);
//             if (s == null)
//             {
//                 return;
//             }
//
//             var when = e.When;
//             var notes = e.Notes;
//             var ticker = e.Ticker;
//
//             await CreateNote(e.UserId, when, notes, ticker);
//         }
//
//         public async Task Handle(StockPurchased e, CancellationToken cancellationToken)
//         {
//             var s = await _storage.GetStock(e.Ticker, e.UserId);
//             if (s == null)
//             {
//                 return;
//             }
//
//             var when = e.When;
//             var notes = e.Notes;
//             var ticker = e.Ticker;
//
//             await CreateNote(e.UserId, when, notes, ticker);
//         }
//
//         public async Task Handle(StockPurchased_v2 e, CancellationToken cancellationToken)
//         {
//             var s = await _storage.GetStock(e.Ticker, e.UserId);
//             if (s == null)
//             {
//                 return;
//             }
//
//             var when = e.When;
//             var notes = e.Notes;
//             var ticker = e.Ticker;
//
//             await CreateNote(e.UserId, when, notes, ticker);
//         }
//
//         private async Task CreateNote(Guid userId, DateTimeOffset when, string notes, string ticker)
//         {
//             if (string.IsNullOrEmpty(notes))
//             {
//                 return;
//             }
//
//             var cmd = new AddNote
//             {
//                 Created = when,
//                 Note = notes,
//                 Ticker = ticker
//             };
//
//             cmd.WithUserId(userId);
//
//             await _mediator.Send(cmd);
//         }
//     }
