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

        private Task Loop(CancellationToken stoppingToken)
        {
            if (_queue.TryDequeue(out var r))
            {
                Console.WriteLine("sending user changed event " + r.UserId);
                return _mediator.Publish(new UserChanged(r.UserId));
            }
            else
            {
                Console.WriteLine("nothing to do ");
                return Task.CompletedTask;
            }
        }
    }
}