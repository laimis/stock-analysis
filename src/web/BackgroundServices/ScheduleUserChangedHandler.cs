using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace web.BackgroundServices
{
    public class ScheduleUserChangedHandler : MediatR.INotificationHandler<ScheduleUserChanged>
    {
        public ScheduleUserChangedHandler(UserChangedService userChangedService)
        {
            _userChangedService = userChangedService;
        }

        private readonly UserChangedService _userChangedService;

        public Task Handle(ScheduleUserChanged e, CancellationToken cancellationToken)
        {
            Console.WriteLine("ScheduleUserChangedHandler");
            
            _userChangedService.Schedule(e);
            
            return Task.CompletedTask;
        }
    }
}