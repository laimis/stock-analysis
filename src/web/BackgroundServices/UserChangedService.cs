using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class UserChangedService : BackgroundService
    {
        private readonly ILogger<UserChangedService> _logger;
        private readonly IMediator _mediator;

        private static readonly ConcurrentQueue<ScheduleUserChanged> _queue = new();
        public static void Schedule(ScheduleUserChanged e) => _queue.Enqueue(e);

        public UserChangedService(
            ILogger<UserChangedService> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        private static readonly TimeSpan _sleepDuration = TimeSpan.FromSeconds(1);

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
                    _logger.LogError("User changed service failed: {exception}", ex);
                }

                await Task.Delay(_sleepDuration, stoppingToken);
            }

            _logger.LogInformation("exec exit");
        }

        private readonly Dictionary<Guid, DateTimeOffset> _userLastIssued = new();

        private Task Loop(CancellationToken stoppingToken)
        {
            while (_queue.TryDequeue(out var r))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }

                _userLastIssued.TryGetValue(r.UserId, out var lastScheduled);
                if (r.When > lastScheduled)
                {
                    _logger.LogInformation("Sending user changed event for {userId}", r.UserId);
                    _userLastIssued[r.UserId] = DateTimeOffset.UtcNow;
                    _mediator.Publish(new UserChanged(r.UserId), stoppingToken);
                }
                else
                {
                    _logger.LogInformation("Skipping scheduling for {userId}, too old", r.UserId);
                }
            }
            
            return Task.CompletedTask;
        }
    }
}