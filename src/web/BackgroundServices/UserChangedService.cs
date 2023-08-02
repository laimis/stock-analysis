using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using MediatR;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices
{
    public class UserChangedService : GenericBackgroundServiceHost
    {
        private readonly IMediator _mediator;
        private readonly ConcurrentQueue<ScheduleUserChanged> _queue = new();
        public void Schedule(ScheduleUserChanged e) => _queue.Enqueue(e);

        public UserChangedService(
            ILogger<UserChangedService> logger,
            IMediator mediator) : base(logger)
        {
            _mediator = mediator;
        }

        private readonly Dictionary<Guid, DateTimeOffset> _userLastIssued = new();

        private static readonly TimeSpan _sleepInterval = TimeSpan.FromSeconds(1);
        protected override TimeSpan SleepDuration { get => _sleepInterval;}

        protected override Task Loop(CancellationToken stoppingToken)
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