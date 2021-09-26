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
            Console.WriteLine("scheduling user changed");

            UserChangedScheduler.Schedule(e);
            
            return Task.CompletedTask;
        }
    }
}