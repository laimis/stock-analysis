using System;
using System.Threading;
using System.Threading.Tasks;
using core.fs.Alerts;
using core.Shared.Adapters.Brokerage;
using Microsoft.Extensions.Logging;

namespace web.BackgroundServices;
public class StopLossServiceHost : GenericBackgroundServiceHost
{
    private readonly IMarketHours _marketHours;
    private readonly MonitoringServices.StopLossMonitoringService _service;

    public StopLossServiceHost(
        ILogger<StopLossServiceHost> logger,
        IMarketHours marketHours,
        MonitoringServices.StopLossMonitoringService stopLossMonitoringService) : base(logger)
    {
        _marketHours = marketHours;
        _service = stopLossMonitoringService;
    }

    protected override TimeSpan GetSleepDuration() =>
        MonitoringServices.nextStopLossRun(DateTimeOffset.UtcNow, _marketHours) - DateTimeOffset.UtcNow;

    protected override async Task Loop(CancellationToken stoppingToken) => await _service.Execute(stoppingToken);
}
