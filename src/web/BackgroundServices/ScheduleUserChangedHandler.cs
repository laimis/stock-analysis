using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace web.BackgroundServices
{
    public class ScheduleUserChangedHandler : MediatR.INotificationHandler<ScheduleUserChanged>
    {
        public Task Handle(ScheduleUserChanged e, CancellationToken cancellationToken)
        {
            UserChangedService.Schedule(e);
            
            return Task.CompletedTask;
        }
    }
}