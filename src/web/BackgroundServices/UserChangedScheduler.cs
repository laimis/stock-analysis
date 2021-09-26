using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class UserChangedScheduler : BackgroundService
    {
        private ILogger<UserChangedScheduler> _logger;
        private IMediator _mediator;

        public static Queue<ScheduleUserChanged> _queue = new Queue<ScheduleUserChanged>();
        public static void Schedule(ScheduleUserChanged e) => _queue.Enqueue(e);

        public UserChangedScheduler(
            ILogger<UserChangedScheduler> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        private const int SHORT_INTERVAL = 1_000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("running user change scheduler");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Loop(stoppingToken);            
                }
                catch(Exception ex)
                {
                    _logger.LogError("Failed:" + ex);
                }

                await Task.Delay(SHORT_INTERVAL, stoppingToken);
            }

            _logger.LogInformation("exec exit");
        }

        private Dictionary<Guid, DateTimeOffset> _userLastIssued = new Dictionary<Guid, DateTimeOffset>();

        private Task Loop(CancellationToken stoppingToken)
        {
            if (_queue.TryDequeue(out var r))
            {
                _userLastIssued.TryGetValue(r.UserId, out var lastScheduled);
                if (r.When > lastScheduled)
                {
                    Console.WriteLine("Sending user changed event " + r.UserId);
                    _userLastIssued[r.UserId] = DateTimeOffset.UtcNow;
                    return _mediator.Publish(new UserChanged(r.UserId));
                }
                else
                {
                    Console.WriteLine("Skipping scheduling, too old");
                }
            }
            
            return Task.CompletedTask;
        }
    }
}